using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using ContentRepository;
using ContentRepository.Storage;
using Messaging;
using Portal.UI.PortletFramework;
using Portal.Virtualization;

namespace Portal.Portlets
{
    public class NotificationDeletePortlet : ContextBoundPortlet
    {
        public NotificationDeletePortlet()
        {
            this.Name = "$NotificationDeletePortlet:PortletDisplayName";
            this.Description = "$NotificationDeletePortlet:PortletDescription";
            this.Category = new PortletCategory("$PortletFramework:Category_Notification", "$PortletFramework:Category_Notification_Description");

            this.HiddenProperties.Add("Renderer");
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (string.IsNullOrEmpty(PortalContext.Current.ActionName) || PortalContext.Current.ActionName.ToLower() != "deletenotification")
                return;
            
            var contentPath = HttpContext.Current.Request["ContentPath"];
            if (string.IsNullOrEmpty(contentPath))
            {
                //subscription for the current content
                contentPath = this.ContextNode.Path;
            }

            var node = Node.LoadNode(contentPath);
            var userPath = HttpContext.Current.Request["UserPath"] ?? string.Empty;
            var user = string.IsNullOrEmpty(userPath) ? User.Current as User : Node.Load<User>(userPath);

            if (node == null || user == null)
                return;

            Subscription.UnSubscribe(user, node);
            
            CallDone(false);
        }
    }
}
