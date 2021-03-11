using System;
using System.Web.Compilation;

namespace Portal
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            BuildManager.GetReferencedAssemblies();
            DMISGlobal.ApplicationStartHandler(sender, e, this);
        }
        protected void Application_End(object sender, EventArgs e)
        {
            DMISGlobal.ApplicationEndHandler(sender, e, this);
        }
        protected void Application_Error(object sender, EventArgs e)
        {
            DMISGlobal.ApplicationErrorHandler(sender, e, this);
        }
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            DMISGlobal.ApplicationBeginRequestHandler(sender, e, this);
        }
        protected void Application_EndRequest(object sender, EventArgs e)
        {
            DMISGlobal.ApplicationEndRequestHandler(sender, e, this);
        }
    }
}
