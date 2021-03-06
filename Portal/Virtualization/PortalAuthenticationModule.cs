using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;
using ContentRepository;
using Preview;
using ContentRepository.Security;
using ContentRepository.Security.ADSync;
using ContentRepository.Storage;
using ContentRepository.Storage.ApplicationMessaging;
using ContentRepository.Storage.Data;
using ContentRepository.Storage.Schema;
using ContentRepository.Storage.Search;
using ContentRepository.Storage.Security;
using Diagnostics;
using Portal.Handlers;
using System.Threading;

namespace Portal.Virtualization
{

    class PortalAuthenticationModule : IHttpModule
    {

        public void Dispose()
        {
            // Nothing to dispose yet...
        }

        public void Init(HttpApplication context)
        {
            FormsAuthentication.Initialize();
            context.AuthenticateRequest += new EventHandler(OnAuthenticateRequest);
            context.EndRequest += new EventHandler(OnEndRequest); // Forms
            context.AuthorizeRequest += new EventHandler(OnAuthorizeRequest);
        }

        void OnAuthorizeRequest(object sender, EventArgs e)
        {
            PortalContext currentPortalContext = PortalContext.Current;
            var d = NodeHead.Get("/Root");

            // deny access for visitors in case of webdav or office protocol requests, if they have no See access to the content
            if (currentPortalContext != null)
            {
                var application = sender as HttpApplication;
                var currentUser = application != null ? application.Context.User.Identity as User : null;

                if (currentUser != null && currentUser.Id == RepositoryConfiguration.VisitorUserId && (currentPortalContext.IsOfficeProtocolRequest || currentPortalContext.IsWebdavRequest))
                {
                    if (!currentPortalContext.IsRequestedResourceExistInRepository ||
                        currentPortalContext.ContextNodeHead == null ||
                        !SecurityHandler.HasPermission(currentPortalContext.ContextNodeHead, PermissionType.See))
                    {
                        AuthenticationHelper.ForceBasicAuthentication(HttpContext.Current);
                    }
                }
            }

            if (currentPortalContext != null && currentPortalContext.IsRequestedResourceExistInRepository)
            {
                var authMode = currentPortalContext.AuthenticationMode;
                //install time
                if (string.IsNullOrEmpty(authMode) && currentPortalContext.Site == null)
                    authMode = "None";

                if (string.IsNullOrEmpty(authMode))
                    authMode = WebConfigurationManager.AppSettings["DefaultAuthenticationMode"];

                bool appPerm = false;
                if (authMode == "Forms")
                {
                    appPerm = currentPortalContext.CurrentAction.CheckPermission();
                }
                else if (authMode == "Windows")
                {
                    currentPortalContext.CurrentAction.AssertPermissions();
                    appPerm = true;
                }
                else
                {
                    throw new NotSupportedException("None authentication is not supported");
                }

                var application = sender as HttpApplication;
                var currentUser = application.Context.User.Identity as User;
                var path = currentPortalContext.RepositoryPath;
                var nodeHead = NodeHead.Get(path);
                var isOwner = nodeHead.CreatorId == currentUser.Id;
                var permissionValue = SecurityHandler.GetPermission(nodeHead, PermissionType.Open);

                if (permissionValue == PermissionValue.Allow && DocumentPreviewProvider.Current.IsPreviewOrThumbnailImage(nodeHead))
                {
                    // In case of preview images we need to make sure that they belong to a content version that
                    // is accessible by the user (e.g. must not serve images for minor versions if the user has
                    // access only to major versions of the content).
                    if (!DocumentPreviewProvider.Current.IsPreviewAccessible(nodeHead))
                        permissionValue = PermissionValue.Deny;
                }
                else if (permissionValue != PermissionValue.Allow && appPerm && DocumentPreviewProvider.Current.HasPreviewPermission(nodeHead))
                {
                    // In case Open permission is missing: check for Preview permissions. If the current Document
                    // Preview Provider allows access to a preview, we should allow the user to access the content.
                    permissionValue = PermissionValue.Allow;
                }

                if (permissionValue != PermissionValue.Allow || !appPerm)
                {
                    switch (authMode)
                    {
                        case "Forms":
                            if (User.Current.IsAuthenticated)
                            {
                                // user is authenticated, but has no permissions: return 403
                                application.Context.Response.StatusCode = 403;
                                application.Context.Response.Flush();
                                application.Context.Response.Close();
                            }
                            else
                            {
                                // let webdav and office protocol handle authentication - in these cases redirecting to a login page makes no sense
                                if (PortalContext.Current.IsWebdavRequest || PortalContext.Current.IsOfficeProtocolRequest)
                                    return;

                                // user is not authenticated and visitor has no permissions: redirect to login page
                                // Get the login page Url (eg. http://localhost:1315/home/login)
                                string loginPageUrl = currentPortalContext.GetLoginPageUrl();
                                // Append trailing slash
                                if (loginPageUrl != null && !loginPageUrl.EndsWith("/"))
                                    loginPageUrl = loginPageUrl + "/";

                                // Cut down the querystring (eg. drop ?Param1=value1@Param2=value2)
                                string currentRequestUrlWithoutQueryString = currentPortalContext.OriginalUri.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.Unescaped);

                                // Append trailing slash
                                if (!currentRequestUrlWithoutQueryString.EndsWith("/"))
                                    currentRequestUrlWithoutQueryString = currentRequestUrlWithoutQueryString + "/";

                                // Redirect to the login page, if neccessary.
                                if (currentRequestUrlWithoutQueryString != loginPageUrl)
                                    application.Context.Response.Redirect(loginPageUrl + "?OriginalUrl=" + System.Web.HttpUtility.UrlEncode(currentPortalContext.OriginalUri.ToString()), true);
                            }
                            break;
                        //case "Windows":
                        //    application.Context.Response.Clear();
                        //    application.Context.Response.Buffer = true;
                        //    application.Context.Response.Status = "401 Unauthorized";
                        //    application.Context.Response.AddHeader("WWW-Authenticate", "NTLM");
                        //    application.Context.Response.End();
                        //    break;
                        default:
                            AuthenticationHelper.DenyAccess(application);
                            break;
                    }
                }
            }
        }

        private bool DispatchBasicAuthentication(HttpApplication application, HttpRequest request)
        {
            var authHeader = PortalContext.Current.BasicAuthHeaders;
            if (authHeader == null || !authHeader.StartsWith("Basic "))
                return false;

            string base64Encoded = authHeader.Substring(6);  // 6: length of "Basic "
            byte[] buff = Convert.FromBase64String(base64Encoded);
            string[] userPass = System.Text.Encoding.UTF8.GetString(buff).Split(":".ToCharArray());
            if (userPass.Length != 2)
            {
                application.Context.User = new PortalPrincipal(User.Visitor);
                return true;
            }
            try
            {
                var username = userPass[0];
                var password = userPass[1];

                // Elevation: we need to load the user here, regardless of the current users permissions
                using (new SystemAccount())
                {
                    application.Context.User = Membership.ValidateUser(username, password)
                        ? new PortalPrincipal(User.Load(username))
                        : new PortalPrincipal(User.Visitor);
                }
            }
            catch (Exception e) //logged
            {
                Logger.WriteException(e);
                application.Context.User = new PortalPrincipal(User.Visitor);
            }

            return true;
        }

        private bool DispatchUploadRequest(HttpApplication application, HttpRequest request)
        {
            // if the request is Upload (POST) request, try to authenticate user
            HttpContext context = HttpContext.Current;
            string receivedUploadToken = request.Form["UploadToken"];
            if (receivedUploadToken != null)
            {
                UploadToken uploadToken = UploadToken.GetUploadToken(new Guid(receivedUploadToken));

                if (uploadToken == null)
                    throw new InvalidOperationException(string.Format("An UploadToken ({0}) has been sent, but a matching user cannot be found.", receivedUploadToken));

                UserAccessProvider.ChangeToSystemAccount();
                User uploadUser = (User)ContentRepository.Storage.Node.LoadNode(uploadToken.UserId);
                UserAccessProvider.RestoreOriginalUser();
                context.User = new PortalPrincipal(uploadUser);

                return true;
            }
            return false;

        }

        void OnAuthenticateRequest(object sender, EventArgs e)
        {
            //DebugThis("begin");

            HttpApplication application = sender as HttpApplication;
            HttpContext context = HttpContext.Current;
            HttpRequest request = context.Request;


            //trace
            bool traceReportEnabled;
            var traceQueryString = request.QueryString["trace"];

            if (!String.IsNullOrEmpty(traceQueryString))
                traceReportEnabled = (traceQueryString == "true") ? true : false;
            else
                traceReportEnabled = RepositoryConfiguration.TraceReportEnabled;

            if(traceReportEnabled)
            {
                var slot = Thread.GetNamedDataSlot(Tracing.OperationTraceDataSlotName);
                var data = Thread.GetData(slot);

                if(data == null)
                    Thread.SetData(slot,new OperationTraceCollector());
            }
            //trace

            if ( DispatchBasicAuthentication(application, request) )
                return;

            if (request.Headers["CharlesCrawler"] == "true")
            {
                application.Context.User = new PortalPrincipal(User.Visitor);
                return;
            }

            if ( DispatchUploadRequest(application, request) )
                return;

            string authenticationType = null;
            string repositoryPath = string.Empty;

            // Get the current PortalContext
            var currentPortalContext = PortalContext.Current;
            if (currentPortalContext != null)
            {
                authenticationType = currentPortalContext.AuthenticationMode;

                //install time
                if (string.IsNullOrEmpty(authenticationType) && currentPortalContext.Site == null)
                    authenticationType = "None";
            }

            // default authentication mode
            if (string.IsNullOrEmpty(authenticationType))
                authenticationType = WebConfigurationManager.AppSettings["DefaultAuthenticationMode"];

            // if no site auth mode, no web.config default, then exception...
            if (string.IsNullOrEmpty(authenticationType))
                throw new ApplicationException("The engine could not determine the authentication mode for this request. This request does not belong to a site, and there was no default authentication mode set in the web.config.");

            switch (authenticationType)
            {
                case "Windows":
                    EmulateWindowsAuthentication(application);
                    SetApplicationUser(application, authenticationType);
                    break;
                case "Forms":
                    application.Context.User = null;
                    CallInternalOnEnter(sender, e);
                    SetApplicationUser(application, authenticationType);
                    break;
                case "None":
                    // "None" authentication: set the Visitor Identity
                    application.Context.User = new PortalPrincipal(User.Visitor);
                    break;
                default:
                    Site site = null;
                    ContentRepository.Storage.Node problemNode = ContentRepository.Storage.Node.LoadNode(repositoryPath);
                    if (problemNode != null)
                    {
                        site = Site.GetSiteByNode(problemNode);
                        if (site != null)
                            authenticationType = site.GetAuthenticationType(application.Context.Request.Url);
                    }
                    string message = null;
                    if (site == null)
                        message = string.Format(HttpContext.GetGlobalResourceObject("Portal", "DefaultAuthenticationNotSupported") as string, authenticationType);
                    else
                        message = string.Format("AuthenticationNotSupportedOnSite", site.Name, authenticationType);
                    throw new NotSupportedException(message);
            }
            //DebugThis("end");
        }

        private static void CallInternalOnEnter(object sender, EventArgs e)
        {
            FormsAuthenticationModule formsAuthenticationModule = new FormsAuthenticationModule();
            MethodInfo formsAuthenticationModuleOnEnterMethodInfo = formsAuthenticationModule.GetType().GetMethod("OnEnter", BindingFlags.Instance | BindingFlags.NonPublic);
            formsAuthenticationModuleOnEnterMethodInfo.Invoke(
                formsAuthenticationModule,
                new object[] { sender, e });
        }

        private static void SetApplicationUser(HttpApplication application, string authenticationType)
        {
            if (application.User == null || !application.User.Identity.IsAuthenticated)
            {
                var visitor = User.Visitor;
                var visitorPrincipal = new PortalPrincipal(visitor);
                application.Context.User = visitorPrincipal;
            }
            else
            {
                string domain, username, fullUsername;
                fullUsername = application.User.Identity.Name;
                int slashIndex = fullUsername.IndexOf('\\');
                if (slashIndex < 0)
                {
                    domain = string.Empty;
                    username = fullUsername;
                }
                else
                {
                    domain = fullUsername.Substring(0, slashIndex);
                    username = fullUsername.Substring(slashIndex + 1);
                }

                User user = null;
                if (authenticationType == "Windows")
                {
                    var widentity = application.User.Identity as WindowsIdentity;   // get windowsidentity object before elevation
                    using (new SystemAccount())
                    {
                        //force relational engine here, because index doesn't exist install time
                        user = User.Load(domain, username, ExecutionHint.ForceRelationalEngine);
                        if (user != null)
                            user.WindowsIdentity = widentity;

                        //create non-existing installer user
                        if (user == null && !string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(username))
                        {
                            application.Application.Add("SNInstallUser", fullUsername);

                            if (PortalContext.Current != null &&
                                PortalContext.Current.Site != null &&
                                Group.Administrators.Members.Count() == 1)
                            {
                                user = User.RegisterUser(fullUsername);
                                DirectoryServices.Common.SyncInitialUserProperties(user);
                            }
                        }
                    }

                    if (user != null)
                        AccessProvider.Current.SetCurrentUser(user);
                }
                else
                {
                    // if forms AD auth and virtual AD user is configured
                    // load virtual user properties from AD
                    var ADProvider = DirectoryProvider.Current;
                    if (ADProvider != null)
                    {
                        if (ADProvider.IsVirtualADUserEnabled(domain))
                        {
                            var virtualUserPath = "/Root/IMS/BuiltIn/Portal/VirtualADUser";
                            using (new SystemAccount())
                            {
                                user = Node.LoadNode(virtualUserPath) as User;
                            }

                            if (user != null)
                            {
                                user.SetProperty("Domain", domain);
                                user.Enabled = true;
                                ADProvider.SyncVirtualUserFromAD(domain, username, user);
                            }
                        }
                        else
                        {
                            using (new SystemAccount())
                            {
                                user = User.Load(domain, username);
                            }
                        }
                    }
                    else
                    {
                        using (new SystemAccount())
                        {
                            user = User.Load(domain, username);
                        }
                    }
                }

                //-- Current user will be the Visitor if the resolved user is not available
                if (user == null || !user.Enabled)
                    user = User.Visitor;

                MembershipExtenderBase.Extend(user);

                var appUser = new PortalPrincipal(user);
                application.Context.User = appUser;
            }
        }

        void OnEndRequest(object sender, EventArgs e)
        {
            //DebugThis("begin:" + HttpContext.Current.Response.StatusCode.ToString());

            HttpApplication application = sender as HttpApplication;
            string authType = application.Context.Items["AuthType"] as string;
            if (authType == "Forms")
            {
                FormsAuthenticationModule formsAuthenticationModule = new FormsAuthenticationModule();
                MethodInfo formsAuthenticationModuleOnEnterMethodInfo =
                    formsAuthenticationModule.GetType().GetMethod("OnLeave", BindingFlags.Instance | BindingFlags.NonPublic);
                formsAuthenticationModuleOnEnterMethodInfo.Invoke(
                    formsAuthenticationModule,
                    new object[] { sender, e });
            }

            Logger.WriteVerbose("PortalAuthenticationModule.OnEndRequest",
                new Dictionary<string, object> {
                    { "Url", HttpContext.Current.Request.Url }, 
                    { "StatusCode", HttpContext.Current.Response.StatusCode }
                });
        }

        private void EmulateWindowsAuthentication(HttpApplication application)
        {


            WindowsIdentity identity = null;

            if (HttpRuntime.UsingIntegratedPipeline)
            {
                WindowsPrincipal user = null;
                if (HttpRuntime.IsOnUNCShare && application.Request.IsAuthenticated)
                {
                    IntPtr applicationIdentityToken = (IntPtr)typeof(System.Web.Hosting.HostingEnvironment).GetProperty("ApplicationIdentityToken", BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod().Invoke(null, null);

                    WindowsIdentity wi = new WindowsIdentity(applicationIdentityToken, application.User.Identity.AuthenticationType, WindowsAccountType.Normal, true);

                    user = new WindowsPrincipal(wi);
                }
                else
                {
                    user = application.Context.User as WindowsPrincipal;
                }

                if (user != null)
                {
                    identity = user.Identity as WindowsIdentity;

                    object[] setPrincipalNoDemandParameters = new object[] { null, false };
                    Type[] setPrincipalNoDemandParameterTypes = new Type[] { typeof(IPrincipal), typeof(bool) };
                    MethodInfo setPrincipalNoDemandMethodInfo = application.Context.GetType().GetMethod("SetPrincipalNoDemand", BindingFlags.Instance | BindingFlags.NonPublic, null, setPrincipalNoDemandParameterTypes, null);
                    setPrincipalNoDemandMethodInfo.Invoke(application.Context, setPrincipalNoDemandParameters);
                }

            }
            else
            {
                HttpWorkerRequest workerRequest =
                    (HttpWorkerRequest)application.Context.GetType().GetProperty("WorkerRequest", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true).Invoke(application.Context, null);

                string logonUser = workerRequest.GetServerVariable("LOGON_USER");
                string authType = workerRequest.GetServerVariable("AUTH_TYPE");

                if (logonUser == null) logonUser = string.Empty;
                if (authType == null) authType = string.Empty;

                if (logonUser.Length == 0 && authType.Length == 0 || authType.ToLower() == "basic")
                {
                    identity = WindowsIdentity.GetAnonymous();
                }
                else
                {
                    identity = new WindowsIdentity(workerRequest.GetUserToken(), authType, System.Security.Principal.WindowsAccountType.Normal, true);
                }
            }


            if (identity != null)
            {
                WindowsPrincipal wp = new WindowsPrincipal(identity);

                object[] setPrincipalNoDemandParameters = new object[] { wp, false };
                Type[] setPrincipalNoDemandParameterTypes = new Type[] { typeof(IPrincipal), typeof(bool) };
                MethodInfo setPrincipalNoDemandMethodInfo = application.Context.GetType().GetMethod("SetPrincipalNoDemand", BindingFlags.Instance | BindingFlags.NonPublic, null, setPrincipalNoDemandParameterTypes, null);
                setPrincipalNoDemandMethodInfo.Invoke(application.Context, setPrincipalNoDemandParameters);
            }


            // return 401 if user is not authenticated:
            //  - application.Context.User might be null for /ContentStore.mvc/GetTreeNodeAllChildren?... request
            //  - currentPortalUser.Id might be startupuserid or visitoruserid if browser did not send 'negotiate' auth header yet
            //  - currentPortalUser might be null if application.Context.User.Identity is null or not an IUser
            IUser currentPortalUser = null;
            if (application.Context.User != null)
                currentPortalUser = application.Context.User.Identity as IUser;

            if ((application.Context.User == null) || (currentPortalUser != null &&
                (currentPortalUser.Id == RepositoryConfiguration.StartupUserId ||
                currentPortalUser.Id == RepositoryConfiguration.VisitorUserId)))
            {
                if (!IsLocalAxdRequest())
                    AuthenticationHelper.DenyAccess(application);
                return;
            }

        }
        private bool IsLocalAxdRequest()
        {
            var req = HttpContext.Current.Request;
            if (!req.IsLocal)
                return false;

            var path = req.Url.LocalPath;
            if (String.Equals(path, "/webresource.axd", StringComparison.OrdinalIgnoreCase))
                return true;
            if (String.Equals(path, "/scriptresource.axd", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}
