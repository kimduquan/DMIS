using System;
using System.Web;
using System.Web.UI.WebControls;
using ContentRepository.Storage.Security;
using Portal.UI;
using Portal.UI.Controls;
using Portal.UI.PortletFramework;
using Diagnostics;
using ContentRepository;
using Content = ContentRepository.Content;

namespace Workflow.UI
{
    public class ConfirmPortlet : ContextBoundPortlet
    {
        //========================================================================================= Constructor

        public ConfirmPortlet()
        {
            Name = "$ConfirmPortlet:PortletDisplayName";
            Description = "$ConfirmPortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.Workflow);
        }

        protected override void CreateChildControls()
        {
            var content = Content.Create(ContextNode);
            var view = ContentView.Create(content, Page, ViewMode.Browse);
            Controls.Add(view);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                var confirmItem = ContextNode as GenericContent;
                if (confirmItem != null)
                {
                    using (new SystemAccount())
                    {
                        confirmItem.SetProperty("Confirmed", 1);
                        confirmItem.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(ex);
            }
        }
    }
}
