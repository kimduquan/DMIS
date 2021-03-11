using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Hosting;
using Preview;
using ContentRepository.Storage.ApplicationMessaging;
using ContentRepository.Storage;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using Portal.ApplicationModel;
using Portal.AppModel;
using System.Diagnostics;
using System.Linq;
using Diagnostics;
using ContentRepository.Storage.Data;
using System.Threading;
using System.Configuration;
using ApplicationModel;
using ContentRepository.Storage.Security;
using ContentRepository;
using ContentRepository.Storage.Schema;
using ContentRepository.Storage.Diagnostics;

namespace Portal.Virtualization
{
    public class PortalContextModule : IHttpModule
    {
        // ============================================================================================ Members
        private static int? _binaryHandlerClientCacheMaxAge;
        private static volatile bool _delayRequests;

        // ============================================================================================ Properties
        private static readonly string DENYCROSSSITEACCESSENABLEDKEY = "DenyCrossSiteAccessEnabled";
        private static bool? _denyCrossSiteAccessEnabled = null;
        public static bool DenyCrossSiteAccessEnabled
        {
            get
            {
                if (!_denyCrossSiteAccessEnabled.HasValue)
                {
                    var section = ConfigurationManager.GetSection(Repository.PORTALSECTIONKEY) as System.Collections.Specialized.NameValueCollection;
                    if (section != null)
                    {
                        var valStr = section[DENYCROSSSITEACCESSENABLEDKEY];
                        if (!string.IsNullOrEmpty(valStr))
                        {
                            bool val;
                            if (bool.TryParse(valStr, out val))
                                _denyCrossSiteAccessEnabled = val;
                        }
                    }
                    if (!_denyCrossSiteAccessEnabled.HasValue)
                        _denyCrossSiteAccessEnabled = true;
                }
                return _denyCrossSiteAccessEnabled.Value;
            }
        }


        // ============================================================================================ IHttpModule
        public void Init(HttpApplication context)
        {
            CounterManager.Reset("DelayingRequests");

            context.BeginRequest += OnEnter;
            context.EndRequest += OnEndRequest;
            context.AuthenticateRequest += OnAuthenticate;
            context.AuthorizeRequest += OnAuthorize;
            context.PreSendRequestHeaders += new EventHandler(OnPreSendRequestHeaders);
        }

        void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            //If the content type (MIME type) of the response should be set
            //to a specific type (e.g. in case of images, fonts, etc.), we set 
            //this variable in the RepositoryFile.Open method for the scope of this request.
            if (HttpContext.Current != null && PortalContext.Current != null && HttpContext.Current.Items.Contains(RepositoryFile.RESPONSECONTENTTYPEKEY) && (string.IsNullOrEmpty(PortalContext.Current.ActionName) || PortalContext.Current.ActionName == "Browse"))
            {
                var contentType = HttpContext.Current.Items[RepositoryFile.RESPONSECONTENTTYPEKEY] as string;
                if (!string.IsNullOrEmpty(contentType))
                    HttpContext.Current.Response.ContentType = contentType;
            }
        }

        void OnEndRequest(object sender, EventArgs e)
        {
            var ctx = ((HttpApplication)sender).Context;
            var originalPath = ctx.Items["OriginalPath"] as string;

            var request = ctx.Request;
            if (originalPath != null)
                ctx.RewritePath(originalPath);

#if WEB
            DetailedLogger.Log("PCM.OnEndRequest {0} {1}", request.RequestType, request.Url); // category: WEB
#endif
        }

        void OnEnter(object sender, EventArgs e)
        {
            // check if messages to process from msmq exceeds configured limit: delay current thread until it goes back to normal levels
            DelayCurrentRequestIfNecessary();

            HttpContext httpContext = (sender as HttpApplication).Context;
            var request = httpContext.Request;

#if WEB
            DetailedLogger.Log("PCM.OnEnter {0} {1}", request.RequestType, request.Url); // category: WEB
#endif

            //trace
            bool traceReportEnabled;
            var traceQueryString = request.QueryString["trace"];

            if (!String.IsNullOrEmpty(traceQueryString))
                traceReportEnabled = (traceQueryString == "true") ? true : false;
            else
                traceReportEnabled = RepositoryConfiguration.TraceReportEnabled;

            if (traceReportEnabled)
            {
                var slot = Thread.GetNamedDataSlot(Tracing.OperationTraceDataSlotName);
                var data = Thread.GetData(slot);

                if (data == null)
                    Thread.SetData(slot, new OperationTraceCollector());
            }
            //trace


            var initInfo = PortalContext.CreateInitInfo(httpContext);

            // Check for forbidden paths (custom request filtering), mainly for phisycal folders in the web folder. 
            // The built-in Request filtering module is not capable of filtering folders only in the root, but let
            // us have folders with the same name somewhere else in the Content Repository.
            if (IsForbiddenFolder(initInfo))
            {
                AuthenticationHelper.ThrowNotFound();
            }

            // check if request came to a restricted site via another site
            if (DenyCrossSiteAccessEnabled)
            {
                if (initInfo.RequestedNodeSite != null && initInfo.RequestedSite != null)
                {
                    if (initInfo.RequestedNodeSite.DenyCrossSiteAccess && initInfo.RequestedSite.Id != initInfo.RequestedNodeSite.Id)
                    {
                        HttpContext.Current.Response.StatusCode = 404;
                        HttpContext.Current.Response.Flush();
                        HttpContext.Current.Response.End();
                        return;
                    }
                }
            }

            // add cache-control headers and handle ismodifiedsince requests
            HandleResponseForClientCache(initInfo);

            PortalContext portalContext = PortalContext.Create(httpContext, initInfo);

            var action = HttpActionManager.CreateAction(portalContext);
            Logger.WriteVerbose("HTTP Action.", CollectLoggedProperties, portalContext);

            action.Execute();
        }

        void OnAuthenticate(object sender, EventArgs e)
        {
            SetThreadCulture();
        }

        void OnAuthorize(object sender, EventArgs e)
        {
            //At this point the user has at least some permissions for the requested content. This means
            //we can respond with a 304 status if the content has not changed, without opening a security hole.

            //This value could be set earlier by the HandleResponseForClientCache method
            //(e.g. because of binaryhandler or application client cache values).
            if (PortalContext.Current.ModificationDateForClient.HasValue)
                HttpHeaderTools.EndResponseForClientCache(PortalContext.Current.ModificationDateForClient.Value);

            //check requested nodehead
            if (PortalContext.Current.ContextNodeHead == null)
                return;

            // Check if the requested content is executable (e.g. an aspx file) and has the correct file type. We must
            // call this here instead of OnEnter because the user must be authenticated for the check algorithm to work.
            CheckExecutableType(PortalContext.Current.ContextNodeHead, PortalContext.Current.ActionName);

            var modificationDate = PortalContext.Current.ContextNodeHead.ModificationDate;

            //If action name is given, do not do shortcircuit (eg. myimage.jpg?action=Edit 
            //should be a server-rendered page) - except if this is an image resizer application.
            if (!string.IsNullOrEmpty(PortalContext.Current.ActionName))
            {
                var remapAction = PortalContext.Current.CurrentAction as RemapHttpAction;
                if (remapAction == null || remapAction.HttpHandlerNode == null)
                    return;

                if (!remapAction.HttpHandlerNode.GetNodeType().IsInstaceOfOrDerivedFrom(typeof(ImgResizeApplication).Name))
                    return;

                //check if the image resizer app was modified since the last request
                if (remapAction.HttpHandlerNode.ModificationDate > modificationDate)
                    modificationDate = remapAction.HttpHandlerNode.ModificationDate;
            }

            //set cache values for images, js/css files
            var cacheSetting = GetCacheHeaderSetting(PortalContext.Current.RequestedUri, PortalContext.Current.ContextNodeHead);
            if (cacheSetting.HasValue)
            {
                HttpHeaderTools.SetCacheControlHeaders(cacheSetting.Value);

                //in case of preview images do NOT return 304, because _undetectable_ permission changes
                //(on the image or on one of its parents) may change the preview image (e.g. display redaction or not).
                if (DocumentPreviewProvider.Current == null || !DocumentPreviewProvider.Current.IsPreviewOrThumbnailImage(PortalContext.Current.ContextNodeHead))
                {
                    //end response, if the content has not changed since the value posted by the client
                    HttpHeaderTools.EndResponseForClientCache(modificationDate);
                }
            }
        }

        public void Dispose()
        {
        }


        // ============================================================================================ Methods
        private static void SetThreadCulture()
        {
            // Set the CurrentCulture and the CurrentUICulture of the current thread based on the site language.
            // If the site language was set to "FallbackToDefault", or was set to an empty value, the thread culture
            // remain unmodified and will contain its default value (based on Web- and machine.config).
            var portalContext = PortalContext.Current;
            var site = portalContext.Site;
            if (site == null)
                return;

            var cultureSet = false;

            //the strongest setting: user property
            if (site.EnableUserBasedCulture)
            {
                var currentUser = User.Current as User;
                if (currentUser != null)
                {
                    cultureSet = TrySetThreadCulture(currentUser.Language);
                }
            }

            //second try: browser based culture
            if (!cultureSet && site.EnableClientBasedCulture)
            {
                // Set language to user's browser settings
                var languages = HttpContext.Current.Request.UserLanguages;

                if (languages != null)
                {
                    foreach (var language in languages.Where(lng => lng != null))
                    {
                        var formattedLang = language.ToLowerInvariant().Trim();

                        // trim the q (quality) value from the end
                        if (formattedLang.IndexOf(';') > 0)
                            formattedLang = formattedLang.Substring(0, formattedLang.IndexOf(';'));

                        cultureSet = TrySetThreadCulture(formattedLang);

                        if (cultureSet)
                            break;
                    }
                }
            }

            // culture is not yet resolved or resolution from user profile or client failed: use site language
            if (!cultureSet)
                TrySetThreadCulture(site.Language);
        }

        private static readonly DateTime _dateTimeMinValue = CultureInfo.InvariantCulture.DateTimeFormat.Calendar.MinSupportedDateTime;
        private static readonly DateTime _dateTimeMaxValue = CultureInfo.InvariantCulture.DateTimeFormat.Calendar.MaxSupportedDateTime;

        private static bool TrySetThreadCulture(string language)
        {
            // If the language was set to a non-empty value, and was not set to fallback, set the thread locale
            // Otherwise do nothing (the ASP.NET engine already set the locale).
            if (string.IsNullOrEmpty(language) || string.Compare(language, "FallbackToDefault", true) == 0)
                return false;

            CultureInfo specificCulture = null;
            try
            {
                specificCulture = CultureInfo.CreateSpecificCulture(language);
            }
            catch (CultureNotFoundException)
            {
                return false;
            }

            Thread.CurrentThread.CurrentCulture = specificCulture;
            Thread.CurrentThread.CurrentUICulture = specificCulture;
            CheckCulture();

            return true;
        }
        private static void CheckCulture()
        {
            var calendar = Thread.CurrentThread.CurrentCulture.DateTimeFormat.Calendar;
            if (calendar.MinSupportedDateTime <= _dateTimeMinValue && calendar.MaxSupportedDateTime >= _dateTimeMaxValue)
                return;

            var logMsg = Thread.CurrentThread.CurrentCulture.Name + ": " + calendar + " is changed to ";
            Calendar[] optCals = Thread.CurrentThread.CurrentCulture.OptionalCalendars;
            foreach (var cal in optCals)
            {
                if (cal.MinSupportedDateTime <= _dateTimeMinValue && cal.MaxSupportedDateTime >= _dateTimeMaxValue)
                {
                    Thread.CurrentThread.CurrentCulture.DateTimeFormat.Calendar = cal;
                    Thread.CurrentThread.CurrentUICulture.DateTimeFormat.Calendar = cal;
                    Logger.WriteInformation(Logger.EventId.NotDefined, logMsg + cal);
                    return;
                }
            }
            Logger.WriteWarning(Logger.EventId.NotDefined, "This locale cannot be used: " + Thread.CurrentThread.CurrentCulture.Name + ". Current culture is assigned to invariant culture.");
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        private static IDictionary<string, object> CollectLoggedProperties(IHttpActionContext context)
        {
            var action = context.CurrentAction;
            var props = new Dictionary<string, object>
                {
                    {"ActionType", action.GetType().Name},
                    {"TargetNode",  action.TargetNode == null ? "[null]" : action.TargetNode.Path},
                    {"AppNode",  action.AppNode == null ? "[null]" : action.AppNode.Path}
                };

            if (action is DefaultHttpAction)
            {
                props.Add("RequestUrl", context.RequestedUrl);
                return props;
            }
            var redirectAction = action as RedirectHttpAction;
            if (redirectAction != null)
            {
                props.Add("TargetUrl", redirectAction.TargetUrl);
                props.Add("EndResponse", redirectAction.EndResponse);
                return props;
            }
            var remapAction = action as RemapHttpAction;
            if (remapAction != null)
            {
                if (remapAction.HttpHandlerType != null)
                    props.Add("HttpHandlerType", remapAction.HttpHandlerType.Name);
                else
                    props.Add("HttpHandlerNode", remapAction.HttpHandlerNode.Path);
                return props;
            }
            var rewriteAction = action as RewriteHttpAction;
            if (rewriteAction != null)
            {
                props.Add("Path", rewriteAction.Path);
                return props;
            }
            return props;
        }

        private bool CheckVisitorPermissions(NodeHead nodeHead)
        {
            using (new SystemAccount())
            {
                return SecurityHandler.HasPermission((IUser)User.Visitor, nodeHead.Path, nodeHead.CreatorId, nodeHead.LastModifierId, PermissionType.See, PermissionType.Open);
            }
        }

        private void HandleResponseForClientCache(PortalContextInitInfo initInfo)
        {
            // binaryhandler
            if (initInfo.BinaryHandlerRequestedNodeHead != null)
            {
                var bhMaxAge = Settings.GetValue(PortalSettings.SETTINGSNAME, PortalSettings.SETTINGS_BINARYHANDLER_MAXAGE, initInfo.RepositoryPath, 0);
                if (bhMaxAge > 0)
                {
                    HttpHeaderTools.SetCacheControlHeaders(bhMaxAge);

                    // We're only handling these if the visitor has permissions to the node
                    if (CheckVisitorPermissions(initInfo.RequestedNodeHead))
                    {
                        // handle If-Modified-Since and Last-Modified headers
                        HttpHeaderTools.EndResponseForClientCache(initInfo.BinaryHandlerRequestedNodeHead.ModificationDate);
                    }
                    else
                    {
                        //otherwise store the value for later use
                        initInfo.ModificationDateForClient = initInfo.BinaryHandlerRequestedNodeHead.ModificationDate;
                    }

                    return;
                }
            }

            if (initInfo.IsWebdavRequest || initInfo.IsOfficeProtocolRequest)
            {
                //HttpHeaderTools.SetCacheControlHeaders(0, HttpCacheability.NoCache);
                //HttpContext.Current.Response.Headers.Add("Cache-Control", "no-store, no-cache, private");
                HttpContext.Current.Response.Headers.Add("Pragma", "no-cache"); //HTTP 1.0
                HttpContext.Current.Response.Headers.Add("Expires", "Sat, 26 Jul 1997 05:00:00 GMT"); // Date in the past
                return;
            }

            // get requested nodehead
            if (initInfo.RequestedNodeHead == null)
                return;

            // if action name is given, do not do shortcircuit (eg. myscript.js?action=Edit should be a server-rendered page)
            if (!string.IsNullOrEmpty(initInfo.ActionName))
                return;

            // **********************************************************
            // Image content check is moved to OnAuthorize event handler, because it needs the
            // fully loaded node. Here we handle only other content - e.g. js/css files.
            // **********************************************************

            if (!initInfo.RequestedNodeHead.GetNodeType().IsInstaceOfOrDerivedFrom(typeof(Image).Name))
            {
                var cacheSetting = GetCacheHeaderSetting(initInfo.RequestUri, initInfo.RequestedNodeHead);
                if (cacheSetting.HasValue)
                {
                    HttpHeaderTools.SetCacheControlHeaders(cacheSetting.Value);

                    // We're only handling these if the visitor has permissions to the node
                    if (CheckVisitorPermissions(initInfo.RequestedNodeHead))
                    {
                        // handle If-Modified-Since and Last-Modified headers
                        HttpHeaderTools.EndResponseForClientCache(initInfo.RequestedNodeHead.ModificationDate);
                    }
                    else
                    {
                        //otherwise store the value for later use
                        initInfo.ModificationDateForClient = initInfo.RequestedNodeHead.ModificationDate;
                    }

                    return;
                }
            }

            // applications
            Application app;

            // elevate to sysadmin, as we are startupuser here, and group 'everyone' should have permissions to application without elevation
            using (new SystemAccount())
            {
                //load the application, or the node itself if it is an application
                if (initInfo.RequestedNodeHead.GetNodeType().IsInstaceOfOrDerivedFrom("Application"))
                    app = Node.LoadNode(initInfo.RequestedNodeHead) as Application;
                else
                    app = ApplicationStorage.Instance.GetApplication(initInfo.ActionName, initInfo.RequestedNodeHead, initInfo.DeviceName);
            }

            if (app == null)
                return;

            var maxAge = app.NumericMaxAge;
            var cacheControl = app.CacheControlEnumValue;

            if (cacheControl.HasValue && maxAge.HasValue)
            {
                HttpHeaderTools.SetCacheControlHeaders(maxAge.Value, cacheControl.Value);

                // We're only handling these if the visitor has permissions to the node
                if (CheckVisitorPermissions(initInfo.RequestedNodeHead))
                {
                    // handle If-Modified-Since and Last-Modified headers
                    HttpHeaderTools.EndResponseForClientCache(initInfo.RequestedNodeHead.ModificationDate);
                }
                else
                {
                    //otherwise store the value for later use
                    initInfo.ModificationDateForClient = initInfo.RequestedNodeHead.ModificationDate;
                }
            }
        }
        private void DelayCurrentRequestIfNecessary()
        {
            // check if messages to process from msmq exceeds configured limit: delay current thread until it goes back to normal levels
            _delayRequests = IsDelayingRequestsNecessary(_delayRequests);
            while (_delayRequests)
            {
                Thread.Sleep(100);
                _delayRequests = IsDelayingRequestsNecessary(_delayRequests);
            }
        }
        private static bool IsDelayingRequestsNecessary(bool requestsCurrentlyDelayed)
        {
            // by default we keep current working mode
            var delayingRequestsNecessary = requestsCurrentlyDelayed;

            // check if we need to switch off/on delaying
            var incomingMessageCount = DistributedApplication.ClusterChannel.IncomingMessageCount;
            if (!requestsCurrentlyDelayed && incomingMessageCount > RepositoryConfiguration.DelayRequestsOnHighMessageCountUpperLimit)
            {
                delayingRequestsNecessary = true;
                //Logger.WriteInformation("Requests are now being delayed (IncomingMessageCount reached configured upper limit: " + incomingMessageCount.ToString() + ")");
            }
            if (requestsCurrentlyDelayed && incomingMessageCount < RepositoryConfiguration.DelayRequestsOnHighMessageCountLowerLimit)
            {
                delayingRequestsNecessary = false;
                //Logger.WriteInformation("Request delaying is now switched off (IncomingMessageCount reached configured lower limit: " + incomingMessageCount.ToString() + ")");
            }

            CounterManager.SetRawValue("DelayingRequests", delayingRequestsNecessary ? 1 : 0);
            return delayingRequestsNecessary;
        }

        private static int? GetCacheHeaderSetting(Uri requestUri, NodeHead requestedNodeHead)
        {
            if (requestUri == null || requestedNodeHead == null)
                return null;

            var extension = Path.GetExtension(requestUri.AbsolutePath).ToLower().Trim(new[] { ' ', '.' });
            var contentType = requestedNodeHead.GetNodeType().Name;

            //shortcut: deal with real files only
            if (string.IsNullOrEmpty(extension) || string.IsNullOrEmpty(contentType))
                return null;

            var cacheHeaderSettings = Settings.GetValue<IEnumerable<CacheHeaderSetting>>(PortalSettings.SETTINGSNAME, PortalSettings.SETTINGS_CACHEHEADERS, requestedNodeHead.Path);
            if (cacheHeaderSettings == null)
                return null;

            foreach (var chs in cacheHeaderSettings)
            {
                //check if one of the criterias does not match
                var extMismatch = !string.IsNullOrEmpty(chs.Extension) && chs.Extension != extension;
                var contentTypeMismach = !string.IsNullOrEmpty(chs.ContentType) && chs.ContentType != contentType;
                var pathMismatch = !string.IsNullOrEmpty(chs.Path) && !requestedNodeHead.Path.StartsWith(RepositoryPath.Combine(chs.Path, "/"));

                if (extMismatch || pathMismatch || contentTypeMismach)
                    continue;

                //found a match
                return chs.MaxAge;
            }

            return null;
        }

        /// <summary>
        /// These are forbidden folders in the web folder that cannot be served to any client.
        /// </summary>
        private static readonly string[] _forbiddenFolders = { "Admin", "TaskManagement", "Tools" };

        private static bool IsForbiddenFolder(PortalContextInitInfo initInfo)
        {
            if (initInfo == null || string.IsNullOrEmpty(initInfo.SiteRelativePath))
                return false;

            // get the first folder name from the path
            var folderNames = initInfo.SiteRelativePath.Trim('/').Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            var firstFolderName = folderNames.Length > 0 ? folderNames[0] : string.Empty;
            
            if (!string.IsNullOrEmpty(firstFolderName) && _forbiddenFolders.Any(fp => string.CompareOrdinal(fp, firstFolderName) == 0))
                return true;

            // if it is a full path
            if (initInfo.SiteRelativePath.StartsWith("/Root/", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(initInfo.SiteUrl))
            {
                // find the site above this content
                var site = PortalContext.Sites.Values.FirstOrDefault(s => s.UrlList.ContainsKey(initInfo.SiteUrl));
                if (site == null) 
                    return false;

                var siteRelative = PortalContext.GetSiteRelativePath(initInfo.SiteRelativePath, site);
                folderNames = siteRelative.Trim('/').Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                firstFolderName = folderNames.Length > 0 ? folderNames[0] : string.Empty;

                if (!string.IsNullOrEmpty(firstFolderName) && _forbiddenFolders.Any(fp => string.CompareOrdinal(fp, firstFolderName) == 0))
                    return true;
            }

            return false;
        }

        private static void CheckExecutableType(NodeHead nodeHead, string actioName)
        {
            if (nodeHead == null)
                return;

            // check if the extension interests us
            if (!Tools.IsExecutableExtension(Path.GetExtension(nodeHead.Name)))
                return;

            // If this is not an action request: if the extension indicates an executable file, 
            // but the type is wrong OR the user does not have Run application 
            // permission: rewrite the action to simply return the text of the file.
            if (string.IsNullOrEmpty(actioName) && (!Tools.IsExecutableType(nodeHead.GetNodeType()) || !SecurityHandler.HasPermission(nodeHead, PermissionType.RunApplication)))
            {
                PortalContext.Current.ActionName = "BinarySpecial";

                // Workaround: at this point we cannot change the action in any other way: we need
                // to rewrite the context to point to the binary highlighter page instead of the
                // executable content itself. This is how we prevent executing the file and allow
                // only showing the text content of the file (if Open permission is present).
                var action = HttpActionManager.CreateAction(PortalContext.Current);
                action.Execute();
            }
        }
    }
}
