using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository;
using Portal.Virtualization;

namespace ApplicationModel
{
    public class DeleteLocalAppAction : PortalAction
    {
        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);

            if (!context.Path.Contains("/(apps)/This/"))
                this.Forbidden = true;
        }

        public override string Uri
        {
            get
            {
                if (Content == null || this.Forbidden)
                    return string.Empty;

                var s = SerializeParameters(GetParameteres());
                var uri = string.Format("{0}?action={1}{2}", Content.Path, "Delete", s);

                if (this.IncludeBackUrl && !string.IsNullOrEmpty(this.BackUri))
                {
                    uri += string.Format("&{0}={1}", PortalContext.BackUrlParamName, System.Uri.EscapeDataString(this.BackUri));
                }

                return uri;
            }
        }
    }
}
