using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ApplicationModel;
using ContentRepository;
using ContentRepository.Storage.Data;
using Portal.Virtualization;

namespace ApplicationModel
{
    public class LogoutAction : ServiceAction
    {
        public override string ServiceName
        {
            get
            {
                return "SmartAppHelper.mvc";
            }
            set
            {
                base.ServiceName = value;
            }
        }

        public override string MethodName
        {
            get
            {
                return "Logout";
            }
            set
            {
                base.MethodName = value;
            }
        }

        public override void Initialize(Content context, string backUri, Application application, object parameters)
        {
            base.Initialize(context, backUri, application, parameters);

            if (PortalContext.Current.AuthenticationMode == "Windows" || !User.Current.IsAuthenticated)
            {
                this.Visible = false;
            }
        }
    }
}
