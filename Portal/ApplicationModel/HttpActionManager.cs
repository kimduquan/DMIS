using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository.Storage;
using Portal.Virtualization;
using System.Web;
using System.IO;
using ContentRepository.Storage.Security;
using ContentRepository.Storage.Data;
using ContentRepository.Storage.Schema;
using System.Configuration;
using ContentRepository.Storage.AppModel;
using System.Diagnostics;
using ApplicationModel;
using ContentRepository;

namespace Portal.AppModel
{
    public static class HttpActionManager
    {
        private static readonly ApplicationQuery Apps = new ApplicationQuery("(apps)", false, false, HierarchyOption.TypeAndPath);

        public static IHttpAction CreateAction(IHttpActionContext context)
        {
            var action = CreateActionPrivate(context, null, null, null, null, null);
            context.CurrentAction = action;
            return action;
        }
        private static IHttpAction CreateActionPrivate(IHttpActionContext actionContext, IHttpActionFactory actionFactory, NodeHead requestedNode, string requestedActionName, string requestedApplicationNodePath, string requestedDevice)
        {
            IHttpAction action = null;

            var factory = actionFactory ?? actionContext.GetActionFactory();
            var contextNode = requestedNode ?? actionContext.GetRequestedNode();
            var actionName = requestedActionName ?? actionContext.RequestedActionName;
            var appNodePath = requestedApplicationNodePath ?? actionContext.RequestedApplicationNodePath;
            var portalContext = (PortalContext)actionContext;

            //================================================= #1: preconditions

            action = GetODataAction(factory, portalContext, contextNode);
            if (action != null)
                return action;

            action = GetFirstRunAction(factory, portalContext, contextNode);
            if (action != null)
                return action;

            // webdav request?
            action = GetWebdavAction(factory, portalContext, contextNode);
            if (action != null)
                return action;

            /*
            //---- Uncomment and recompile to support *.SVC on IIS5.1
            action = (GetIIS5SVCRequestAction(portalContext, httpContext));
            if (action != null)
                return action;
            */

            //----------------------------------------------- forward to start page if context is a Site

            action = GetSiteStartPageAction(factory, portalContext, contextNode);
            if (action != null)
                return action;

            //----------------------------------------------- smart url

            action = GetSmartUrlAction(factory, portalContext, contextNode);
            if (action != null)
                return action;

            //----------------------------------------------- outer resource

            action = GetExternalResourceAction(factory, portalContext, contextNode);
            if (action != null)
                return action;

            //----------------------------------------------- context is external page

            action = GetExternalPageAction(factory, portalContext, contextNode, actionName, appNodePath);
            if (action != null)
                return action;

            //----------------------------------------------- context is IHttpHandlerNode

            if (string.IsNullOrEmpty(actionName))
            {
                action = GetIHttpHandlerAction(factory, portalContext, contextNode, contextNode);
                if (action != null)
                    return action;
            }

            //----------------------------------------------- default context action

            action = GetDefaultContextAction(factory, portalContext, contextNode, actionName, appNodePath);
            if (action != null)
                return action;

            //================================================= #2: FindApplication(node, action);

            var appNode = FindApplication(contextNode, actionName, appNodePath, actionContext.DeviceName);
            if (appNode == null)
                return factory.CreateRewriteAction(actionContext, contextNode, null, GetRewritePath(contextNode, portalContext));

            portalContext.BackwardCompatibility_SetPageRepositoryPath(appNode.Path);

            //-----------------------------------------------

            //TODO: appNode external page check?

            //----------------------------------------------- AppNode is IHttpHandlerNode 

            action = GetIHttpHandlerAction(factory, portalContext, contextNode, appNode);
            if (action != null)
                return action;

            //----------------------------------------------- page and site

            return factory.CreateRewriteAction(actionContext, contextNode, appNode, GetRewritePath(appNode, portalContext));
        }


        private static IHttpAction GetFirstRunAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode)
        {
            //TODO: this is for WebPI first time execution, not really side by side friendly reconsideration needed.
            if (portalContext.Site == null)
            {
                Uri uri = portalContext.OwnerHttpContext.Request.Url;
                string uriPath = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
                string installTest = Path.Combine(uri.Authority, "/install/").ToLower();
                string lowerUrl = uri.ToString().ToLower();

                if (!lowerUrl.EndsWith("/default.aspx") &&
                    !lowerUrl.EndsWith("/config.aspx") &&
                    lowerUrl.IndexOf(installTest) < 0 &&
                    !uriPath.EndsWith(".axd"))
                {
                    var appName = HttpRuntime.AppDomainAppVirtualPath;
                    if (!appName.EndsWith("/"))
                        appName = appName + "/";

                    return factory.CreateRedirectAction(portalContext, contextNode, null, 
                        appName + "IISConfig/Config.aspx", false, false);
                }
            }
            return null;
        }
        private static IHttpAction GetIIS5SVCRequestAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode)
        {
            // Rewrite path (create PathInfo) if service called on IIS5
            var httpContext = portalContext.OwnerHttpContext;
            const int XP_MAJOR_VERSION_NUMBER = 5;
            if (httpContext.Request.Path.Contains(".svc") && Environment.OSVersion.Version.Major == XP_MAJOR_VERSION_NUMBER)
            {
                string filePath, pathInfo, queryString;
                string originalPath = httpContext.Request.Path;

                string[] uriElements = System.Text.RegularExpressions.Regex.Split(originalPath, @".svc/");

                filePath = uriElements[0];
                if (!filePath.EndsWith(".svc", StringComparison.InvariantCultureIgnoreCase))
                    filePath = string.Concat(filePath, ".svc");

                pathInfo = uriElements.Length > 1 ? uriElements[1] : String.Empty;
                string qs = httpContext.Request.Url.Query;
                queryString = string.IsNullOrEmpty(qs) ? string.Empty : qs.Substring(1);

                httpContext.RewritePath(filePath, pathInfo, queryString);

                return factory.CreateRewriteAction(portalContext, contextNode, null, filePath, pathInfo, queryString);
            }
            return null;
        }
        private static IHttpAction GetSiteStartPageAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode)
        {
            if (contextNode == null)
                return null;
            if (!contextNode.GetNodeType().IsInstaceOfOrDerivedFrom("Site"))
                return null;
            //var startPage = portalContext.Site.StartPage;
            Node startPage = null;

            using (new SystemAccount())
            {
                var contextSite = Node.Load<Site>(contextNode.Id);
                if (contextSite != null && (portalContext.ActionName == null || portalContext.ActionName.ToLower() == "browse"))
                    startPage = contextSite.StartPage;
                if (startPage == null)
                    return null;
            }

            var relPath = startPage.Path.Replace(portalContext.Site.Path, "");
            return factory.CreateRedirectAction(portalContext, contextNode, null, relPath, false, true);
        }
        private static IHttpAction GetSmartUrlAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode)
        {
            string smartUrl = GetSmartUrl(portalContext);
            if (smartUrl != null)
                return factory.CreateRedirectAction(portalContext, contextNode, null, smartUrl, false, true);
            return null;
        }
        private static IHttpAction GetExternalResourceAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode)
        {
            if (contextNode == null)
            {
                return factory.CreateDefaultAction(portalContext, contextNode, null);
                //return CreateApplication(actionContext, contextNode, null, new RewriteApp { TargetPath = actionContext.RequestedUrl });
            }
            return null;
        }
        private static IHttpAction GetExternalPageAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode, string actionName, string appNodePath)
        {
            if (contextNode == null)
                return null;
            if (actionName != null)
                return null;
            if (appNodePath != null)
                return null;

            string outerUrl = null;
            AccessProvider.ChangeToSystemAccount();
            try
            {
                Page page = Node.LoadNode(contextNode.Id) as Page;
                if (page != null)
                    if (Convert.ToBoolean((page["IsExternal"])))
                        outerUrl = page.GetProperty<string>("OuterUrl");
            }
            finally
            {
                AccessProvider.RestoreOriginalUser();
            }
            if (outerUrl != null)
                return factory.CreateRedirectAction(portalContext, contextNode, null, outerUrl, false, true);
            return null;
        }
        private static IHttpAction GetWebdavAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode)
        {
            if (!portalContext.IsWebdavRequest)
                return null;

            return GetIHttpHandlerAction(factory, portalContext, contextNode, typeof(Services.WebDav.WebDavHandler));
        }
        private static IHttpAction GetODataAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode)
        {
            var uri = portalContext.RequestedUri;
            if(!uri.PathAndQuery.StartsWith("/odata.svc", StringComparison.OrdinalIgnoreCase))
                return null;
            return GetIHttpHandlerAction(factory, portalContext, contextNode, typeof(Portal.OData.ODataHandler));
        }

        private static IHttpAction GetIHttpHandlerAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode, Type httpHandlerType)
        {
            return factory.CreateRemapAction(portalContext, contextNode, null, httpHandlerType);
        }
        private static IHttpAction GetIHttpHandlerAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode, NodeHead handlerNode)
        {
            var nodeType = handlerNode.GetNodeType();
            Type appType = TypeHandler.GetType(nodeType.ClassName);
            if (typeof(IHttpHandler).IsAssignableFrom(appType))
                return factory.CreateRemapAction(portalContext, contextNode, null, handlerNode);
            return null;
        }
        private static IHttpAction GetDefaultContextAction(IHttpActionFactory factory, PortalContext portalContext, NodeHead contextNode, string actionName, string appNodePath)
        {
            if (String.IsNullOrEmpty(actionName) && String.IsNullOrEmpty(appNodePath))
            {
                if(!String.IsNullOrEmpty(portalContext.QueryStringNodePropertyName))
                    return factory.CreateDownloadAction(portalContext, contextNode, null, GetRewritePath(contextNode, portalContext), portalContext.QueryStringNodePropertyName);
                var nodeType = contextNode.GetNodeType();
                if (nodeType.IsInstaceOfOrDerivedFrom("Page"))
                    return factory.CreateRewriteAction(portalContext, contextNode, null, GetRewritePath(contextNode, portalContext));
                if (nodeType.IsInstaceOfOrDerivedFrom("File"))
                    return factory.CreateDownloadAction(portalContext, contextNode, null, GetRewritePath(contextNode, portalContext), PortalContext.DefaultNodePropertyName);
            }
            return null;
        }

        //---------------------------------------------------------------------

        private static NodeHead FindApplication(NodeHead requestedNodeHead, string actionName, string appNodePath, string device)
        {
            if (appNodePath != null)
                return NodeHead.Get(appNodePath);
            if (String.IsNullOrEmpty(actionName))
                actionName = "Browse";

            Content content;

            using (new SystemAccount())
            {
                content = Content.Load(requestedNodeHead.Id);

                // self dispatch
                var genericContent = content.ContentHandler as GenericContent;
                if (genericContent != null)
                {
                    var selfDispatchApp = genericContent.GetApplication(actionName);
                    if (selfDispatchApp != null)
                        return selfDispatchApp;
                }

                //bool appExists;
                //var specificappname = String.Join("-", new string[] { actionName, device });
                //var app = ApplicationStorage.Instance.GetApplication(specificappname, content, out appExists);

                //if (!appExists)
                //    app = ApplicationStorage.Instance.GetApplication(actionName, content, out appExists);

                //if (!string.IsNullOrEmpty(actionName) && !appExists)
                //{
                //    throw new UnknownActionException(string.Format("Action '{0}' does not exist", actionName), actionName);
                //}
                bool appExists;
                var app = ApplicationStorage.Instance.GetApplication(actionName, content, out appExists, device);

                if (!string.IsNullOrEmpty(actionName) && !appExists)
                    throw new UnknownActionException(string.Format("Action '{0}' does not exist", HttpUtility.HtmlEncode(actionName)), actionName);

                return app != null ? NodeHead.Get(app.Id) : null;
            }
        }

        private static string _presenterFolderName = null;
        public static string PresenterFolderName
        {
            get
            {
                if (_presenterFolderName == null)
                    _presenterFolderName = ConfigurationManager.AppSettings["PresenterFolderName"] ?? "(apps)";
                return _presenterFolderName;
            }
        }

        //---------------------------------------------------------------------

        private static string GetRewritePath(NodeHead appNodeHead, PortalContext portalContext)
        {
            if (!String.IsNullOrEmpty(portalContext.QueryStringNodePropertyName))
                return appNodeHead.Path;
            
            NodeType contextNodeType = appNodeHead.GetNodeType();

            if (contextNodeType.IsInstaceOfOrDerivedFrom("Page"))
                return appNodeHead.Path + PortalContext.InRepositoryPageSuffix;
            //else if (contextNodeType.IsInstaceOfOrDerivedFrom("GenericContent") && !contextNodeType.IsInstaceOfOrDerivedFrom("File"))
            //    return appNodeHead.Path + PortalContext.InRepositoryPageSuffix;
            else if (contextNodeType.IsInstaceOfOrDerivedFrom("Site"))
                throw new NotSupportedException("/*!!!*/");

            return appNodeHead.Path;
        }

        private static string GetSmartUrl(PortalContext portalContext)
        {
            if (portalContext == null)
                throw new ArgumentNullException("portalContext");

            if (portalContext.SiteRelativePath == null)
                return null;

            if (portalContext.Site == null)
                return null;

            if (portalContext.Site.Path == null)
                return null;

            var siteRelativePath = portalContext.SiteRelativePath.ToLowerInvariant();
            var sitePath = portalContext.Site.Path.ToLowerInvariant();

            string smartUrlTargetPath;

            PortalContext.SmartUrls.TryGetValue(string.Concat(sitePath, ":", siteRelativePath), out smartUrlTargetPath);

            if (smartUrlTargetPath == null)
                return null;

            string resolvedSmartUrl = string.Concat(
                portalContext.OriginalUri.Scheme,
                "://",
                portalContext.SiteUrl,
                smartUrlTargetPath,
                portalContext.OriginalUri.Query
            );

            return resolvedSmartUrl;
        }

    }
}
