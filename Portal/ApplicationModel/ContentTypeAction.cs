using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ApplicationModel;
using Diagnostics;
using Portal.Virtualization;
using ContentRepository.Storage.Security;
using ContentRepository.Storage.Schema;

namespace ApplicationModel
{
    public class ContentTypeAction : UrlAction
    {
        public override string Uri
        {
            get
            {
                if (Content == null || this.Forbidden)
                    return string.Empty;

                try
                {
                    if (!SecurityHandler.HasPermission(Content.ContentType, PermissionType.Save))
                        return string.Empty;

                    var s = SerializeParameters(GetParameteres());

                    //we provide the edit action of the CTD here
                    var uri = string.Format("{0}?action={1}{2}", Content.ContentType.Path, "Edit", s);

                    if (this.IncludeBackUrl && !string.IsNullOrEmpty(this.BackUri))
                    {
                        uri += string.Format("&{0}={1}", PortalContext.BackUrlParamName, System.Uri.EscapeDataString(this.BackUri));
                    }

                    return uri;
                }
                catch (SecurityException ex)
                {
                    Logger.WriteException(ex);
                }

                return string.Empty;
            }
        }
    }
}
