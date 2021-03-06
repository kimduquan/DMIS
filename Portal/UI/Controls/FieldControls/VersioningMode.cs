using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ContentRepository.Storage;
using ContentRepository;


namespace Portal.UI.Controls
{
	[ToolboxData("<{0}:VersioningMode ID=\"VersioningMode1\" runat=server></{0}:VersioningMode>")]
    public class VersioningMode : FieldControl, INamingContainer, ITemplateFieldControl
	{
		private string _text = string.Empty;

		// ------------------------------------------------------------------ Properties

		// ------------------------------------------------------------------ Constructor
		public VersioningMode()
		{
			ReadOnly = true;
		}
		// ------------------------------------------------------------------ Methods
		public override void SetData(object data)
		{
            #region template

            if ((!UseBrowseTemplate && !UseEditTemplate) && !UseInlineEditTemplate)
                return;

            // synchronize data with controls are given in the template
            SetTitleAndDescription();

            #endregion
		}
		public override object GetData()
		{
			return _text;
		}
		// ------------------------------------------------------------------ Events
		protected override void OnInit(EventArgs e)
		{
			base.OnInit(e);
		}
		protected override void RenderContents(HtmlTextWriter writer)
		{
            #region template

            if (UseBrowseTemplate || UseEditTemplate || UseInlineEditTemplate)
            {
                base.RenderContents(writer);
                return;
            }

            #endregion
			RenderSimple(writer);
		}
        public override object Data
        {
            get
            {
                if (Content.ContentHandler is GenericContent)
                {
                    var gc = (Content.ContentHandler as GenericContent);
                    return gc.VersioningMode;
                }

                return base.Data;
            }
        }

		private void RenderSimple(HtmlTextWriter writer)
		{
            writer.Write(Data);
            //if (Content.ContentHandler is GenericContent)
            //{
            //    GenericContent gc = (Content.ContentHandler as GenericContent);
            //    if (gc != null)
            //        writer.Write(gc.VersioningMode);
            //}
		}

        #region ITemplateFieldControl members

        public Control GetInnerControl()
        {
            return this.FindControlRecursive(InnerControlID) as TextBox;
        }
        public Control GetLabelForDescription()
        {
            return this.FindControlRecursive(DescriptionControlID) as Label;
        }
        public Control GetLabelForTitleControl()
        {
            return this.FindControlRecursive(TitleControlID) as Label;
        }

        #endregion
    }
}