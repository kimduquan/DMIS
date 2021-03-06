using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository;
using System.Web;
using ContentRepository.Storage;
using ContentRepository.Schema;
using Portal.Virtualization;
using ApplicationModel;
using Diagnostics;

namespace Portal.Handlers
{
    [ContentHandler]
    public class HttpHandlerApplication : Application, IHttpHandler
    {
        public HttpHandlerApplication(Node parent) : this(parent, null) { }
        public HttpHandlerApplication(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected HttpHandlerApplication(NodeToken nt) : base(nt) { }

        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {
            var httpHandlerAction = this.CreateAction(Content.Create(PortalContext.Current.ContextNode), null, null) as IHttpHandler;
            if (httpHandlerAction != null)
                httpHandlerAction.ProcessRequest(context);
        }
    }
}
