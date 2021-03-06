using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using ContentRepository;
using ContentRepository.Fields;
using ContentRepository.i18n;

namespace Portal.UI.Controls
{
    [ToolboxData("<{0}:ApprovingModeChoice ID=\"ApprovingModeChoice1\" runat=server></{0}:ApprovingModeChoice>")]
    public class ApprovingModeChoice : DropDown
    {
        private readonly string InheritedValueLabelID = "InheritedValueLabel";
        private readonly string InheritedValuePlaceholderID = "plcInheritedInfo";
        private readonly Label _inheritedValueLabel;

        //=========================================================================== Constructor

        public ApprovingModeChoice()
        {
            _inheritedValueLabel = new Label { ID = InheritedValueLabelID };
        }

        //=========================================================================== Overrides

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (UseBrowseTemplate || UseEditTemplate || UseInlineEditTemplate)
                return;

            Controls.Add(_inheritedValueLabel);
        }

        public override void SetData(object data)
        {
            base.SetData(data);

            if (UseBrowseTemplate || UseEditTemplate || UseInlineEditTemplate)
            {
                var inheritedLabel = GetInheritedValueLabel();
                if (inheritedLabel == null)
                    return;

                inheritedLabel.Text = GetInheritedLabelText(true);
                if (!string.IsNullOrEmpty(inheritedLabel.Text))
                {
                    var inheritedPlc = GetInheritedValuePlaceholder();
                    if (inheritedPlc != null)
                        inheritedPlc.Visible = true;
                }
            }
            else
            {
                _inheritedValueLabel.Text = GetInheritedLabelText(false);
            }
        }

        protected override void RenderContents(HtmlTextWriter writer)
        {
            base.RenderContents(writer);

            if (UseBrowseTemplate || UseEditTemplate || UseInlineEditTemplate)
                return;

            writer.Write("<br />");
            _inheritedValueLabel.RenderControl(writer);
        }

        protected override void FillBrowseControls()
        {
            base.FillBrowseControls();

            var ic = GetBrowseControl() as Label;
            if (ic == null)
                return;

            var data = this.GetData() as List<string>;
            if (data == null)
                return;

            if (data.Count == 1 && data[0] == "0")
            {
                var gc = this.Content == null ? null : this.Content.ContentHandler as GenericContent;
                var parentValue = gc == null ? string.Empty : gc.InheritableApprovingMode.ToString("g");

                ic.Text += ": " + parentValue;
            }
        }

        //===========================================================================

        private string GetInheritedLabelText(bool onlyValue)
        {
            if (ListControl.SelectedIndex > 0)
                return string.Empty;

            var gc = this.Content == null ? null : this.Content.ContentHandler as GenericContent;
            var parentValue = gc == null ? string.Empty : ((int)gc.InheritableApprovingMode).ToString();

            if (string.IsNullOrEmpty(parentValue))
                return parentValue;

            //get the localized value of the setting
            parentValue = ((ChoiceFieldSetting)this.Field.FieldSetting).Options.First(co => co.Value == parentValue).Text;

            return onlyValue ? parentValue : ResourceManager.Current.GetString("Ctd-GenericContent", "VersioningApprovingControls-Value") + ": " + parentValue;
        }

        public Label GetInheritedValueLabel()
        {
            return this.FindControlRecursive(InheritedValueLabelID) as Label;
        }

        public PlaceHolder GetInheritedValuePlaceholder()
        {
            return this.FindControlRecursive(InheritedValuePlaceholderID) as PlaceHolder;
        }
    }
}
