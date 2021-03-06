using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using ContentRepository.Fields;
using Diagnostics;
using System.Web.UI.WebControls;

namespace Portal.UI.Controls
{
    [ToolboxData("<{0}:Currency ID=\"Currency1\" runat=server></{0}:Currency>")]
    public class Currency : Number
    {
        protected string CurrencyControlID = "LabelForCurrency";

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if ((!UseBrowseTemplate && !UseEditTemplate) && !UseInlineEditTemplate) 
                return;

            var lblCurrency = this.FindControlRecursive(CurrencyControlID) as Label;
            if (lblCurrency != null)
            {
                lblCurrency.Text = GetCurrencySymbol();
            }
        }

        protected override void RenderSimple(HtmlTextWriter writer)
        {
            RenderCurrencySymbol(writer);

            base.RenderSimple(writer);
        }

        protected override void RenderEditor(HtmlTextWriter writer)
        {
            RenderCurrencySymbol(writer);

            base.RenderEditor(writer);
        }

        private void RenderCurrencySymbol(HtmlTextWriter writer)
        {
            var cs = GetCurrencySymbol();

            if (!string.IsNullOrEmpty(cs))
                writer.Write(cs + " ");
        }

        private string GetCurrencySymbol()
        {
            var cfs = this.Field.FieldSetting as CurrencyFieldSetting;
            
            return cfs == null 
                ? string.Empty 
                : CurrencyFieldSetting.GetCurrencySymbol(cfs.Format);
        }
    }
}
