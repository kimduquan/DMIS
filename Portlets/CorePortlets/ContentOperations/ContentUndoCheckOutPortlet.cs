using System;
using ContentRepository;
using Diagnostics;
using Portal.UI.PortletFramework;
using Portal.Virtualization;

namespace Portal.Portlets
{
    public class ContentUndoCheckOutPortlet : ContextBoundPortlet
    {
        public ContentUndoCheckOutPortlet()
        {
            this.Name = "$ContentUndoCheckOutPortlet:PortletDisplayName";
            this.Description = "$ContentUndoCheckOutPortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.ContentOperation);
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            var genericContent = GetContextNode() as GenericContent;
            if (genericContent != null)
            {
                try
                {
                    //take action only if the action name is correct
                    if (!string.IsNullOrEmpty(PortalContext.Current.ActionName) &&
                        (PortalContext.Current.ActionName.ToLower() == "undocheckout" ||
                         PortalContext.Current.ActionName.ToLower() == "forceundocheckout"))
                        genericContent.UndoCheckOut();
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                }
            }

            CallDone();
        }
    }
}
