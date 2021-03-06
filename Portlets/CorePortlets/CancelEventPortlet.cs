using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Portal.UI.PortletFramework;
using Portal.Portlets.ContentHandlers;

namespace Portal.Portlets
{
    public class CancelEventPortlet : ContentDeletePortlet
    {
        public CancelEventPortlet()
        {
            this.Name = "$CancelEventPortlet:PortletDisplayName";
            this.Description = "$CancelEventPortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.ContentOperation);
        }

        protected override void MessageControlButtonsAction(object sender, System.Web.UI.WebControls.CommandEventArgs e)
        {
            if (e.CommandName == "Yes")
            {
                var subs = ContextNode as EventRegistrationFormItem;
                subs.SendCancellationMail();
            }
            base.MessageControlButtonsAction(sender, e);
        }
    }
}
