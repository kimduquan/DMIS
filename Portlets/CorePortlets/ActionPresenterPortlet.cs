using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls.WebParts;
using ContentRepository.i18n;
using Diagnostics;
using Portal.UI.PortletFramework;
using Portal.UI.Controls;

namespace Portal.Portlets
{
    public class ActionPresenterPortlet : ContextBoundPortlet
    {
        private const string ActionPresenterPortletClass = "ActionPresenterPortlet";

        public enum IncludeBackUrlMode { Default, True, False }

        public ActionPresenterPortlet()
        {
            Name = "$ActionPresenterPortlet:PortletDisplayName";
            Description = "$ActionPresenterPortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.Portal);

            this.HiddenProperties.Add("Renderer");
        }

        private string _controlPath = "/Root/System/SystemPlugins/Controls/ActionPresenter.ascx";

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(PORTLETFRAMEWORK_CLASSNAME, RENDERER_DISPLAYNAME)]
        [LocalizedWebDescription(PORTLETFRAMEWORK_CLASSNAME, RENDERER_DESCRIPTION)]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(100)]
        [Editor(typeof(ViewPickerEditorPartField), typeof(IEditorPartField))]
        [ContentPickerEditorPartOptions(ContentPickerCommonType.Ascx)]
        public string ControlPath
        {
            get { return _controlPath; } 
            set { _controlPath = value; }
        }

        // portlet uses custom ascx, hide renderer property
        [WebBrowsable(false), Personalizable(true)]
        public override string Renderer { get; set; }

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(ActionPresenterPortletClass, "Prop_ActionName_DisplayName")]
        [LocalizedWebDescription(ActionPresenterPortletClass, "Prop_ActionName_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(110)]
        public string ActionName { get; set; }

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(ActionPresenterPortletClass, "Prop_ActionText_DisplayName")]
        [LocalizedWebDescription(ActionPresenterPortletClass, "Prop_ActionText_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(120)]
        public string ActionText { get; set; }

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(ActionPresenterPortletClass, "Prop_ParameterString_DisplayName")]
        [LocalizedWebDescription(ActionPresenterPortletClass, "Prop_ParameterString_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(130)]
        public string ParameterString { get; set; }

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(ActionPresenterPortletClass, "Prop_IconUrl_DisplayName")]
        [LocalizedWebDescription(ActionPresenterPortletClass, "Prop_IconUrl_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(140)]
        [Editor(typeof(ContentPickerEditorPartField), typeof(IEditorPartField))]
        [ContentPickerEditorPartOptions(ContentPickerCommonType.Icon)]
        public string IconUrl { get; set; }

        //default value is true
        private bool _iconVisible = true;

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(ActionPresenterPortletClass, "Prop_IconVisible_DisplayName")]
        [LocalizedWebDescription(ActionPresenterPortletClass, "Prop_IconVisible_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(150)]
        public bool IconVisible 
        {
            get { return _iconVisible; }
            set { _iconVisible = value; } 
        }

        [WebBrowsable(true)]
        [Personalizable(true)]
        [LocalizedWebDisplayName(ActionPresenterPortletClass, "Prop_IncludeBackUrl_DisplayName")]
        [LocalizedWebDescription(ActionPresenterPortletClass, "Prop_IncludeBackUrl_Description")]
        [WebCategory(EditorCategory.UI, EditorCategory.UI_Order)]
        [WebOrder(160)]
        public IncludeBackUrlMode IncludeBackUrl { get; set; }

        private ActionLinkButton _actionLink;
        protected ActionLinkButton ActionLink
        {
            get { return _actionLink ?? (_actionLink = this.FindControlRecursive("ActionLink") as ActionLinkButton); }
        }

        //================================================================ Overrides

        protected override void CreateChildControls()
        {
            Controls.Clear();

            try
            {
                var viewControl = Page.LoadControl(ControlPath) as UserControl;
                if (viewControl != null)
                {
                    Controls.Add(viewControl);
                    SetParameters();
                }
            }
            catch (Exception exc)
            {
                Logger.WriteException(exc);
            }

            ChildControlsCreated = true;
        }

        //================================================================ Helper methods

        private void SetParameters()
        {
            if (ActionLink == null)
                return;

            ActionLink.ActionName = ActionName;
            ActionLink.ParameterString = ParameterString;
            ActionLink.IconUrl = IconUrl;
            ActionLink.IconVisible = IconVisible;

            if (!string.IsNullOrEmpty(ActionText))
                ActionLink.Text = HttpUtility.HtmlEncode(ResourceManager.Current.GetString(ActionText));

            if (this.IncludeBackUrl != IncludeBackUrlMode.Default)
                ActionLink.IncludeBackUrl = this.IncludeBackUrl == IncludeBackUrlMode.True;

            var ctx = GetContextNode();
            if (ctx != null)
                ActionLink.NodePath = ctx.Path;
        }
    }
}
