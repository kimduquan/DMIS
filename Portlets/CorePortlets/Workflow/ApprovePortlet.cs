using System;
using System.Web.UI;
using Portal.UI.Controls;
using Portal.UI.PortletFramework;
using System.Web.UI.WebControls;
using Portal.UI;
using Repo = ContentRepository;

namespace Workflow.UI
{
    public class ApprovePortlet : ContextBoundPortlet
    {
        public ApprovePortlet()
        {
            Name = "$ApprovePortlet:PortletDisplayName";
            Description = "$ApprovePortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.Workflow);
        }

        private Button _approveButton;
        protected Button ApproveButton
        {
            get
            {
                return _approveButton ?? (_approveButton = this.FindControlRecursive("Approve") as Button);
            }
        }

        private Button _rejectButton;
        protected Button RejectButton
        {
            get
            {
                return _rejectButton ?? (_rejectButton = this.FindControlRecursive("Reject") as Button);
            }
        }

        protected override void CreateChildControls()
        {
            var content = Repo.Content.Create(ContextNode);

            if (ContextNode.NodeType.IsInstaceOfOrDerivedFrom("ApprovalWorkflowTask"))
            {
                ContentView view = null;
                if (!string.IsNullOrEmpty(Renderer))
                    view = ContentView.Create(content, Page, ViewMode.Browse, Renderer);

                if (view == null)
                    view = ContentView.Create(content, Page, ViewMode.Browse);

                Controls.Add(view);

                if (RejectButton != null)
                    RejectButton.Click += RejectButton_Click;

                if (ApproveButton != null)
                    ApproveButton.Click += ApproveButton_Click;
            }

            ChildControlsCreated = true;
        }

        protected override void RenderWithAscx(HtmlTextWriter writer)
        {
            base.RenderContents(writer);
        }

        private void RejectButton_Click(object sender, EventArgs e)
        {
            ContextNode["Result"] = "no";
            ContextNode.Save();
            CallDone();
        }

        private void ApproveButton_Click(object sender, EventArgs e)
        {
            ContextNode["Result"] = "yes";
            ContextNode.Save();
            CallDone();
        }
    }
}
