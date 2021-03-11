using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Portal.UI;
using Portal.Virtualization;

namespace ApplicationModel
{
    public abstract class PortalAction : ActionBase
    {
        public virtual string IconTag
        {
            get
            {
                return IconHelper.RenderIconTag(Icon, null);
            }
        }

        public virtual string SiteRelativePath
        {
            get
            {
                return PortalContext.GetSiteRelativePath(Content.Path);
            }
        }

        protected static string ContinueUri(string uri)
        {
            if (uri.Contains("?"))
                uri += "&";
            else
                uri += "?";

            return uri;
        }
    }
}
