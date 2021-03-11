using System.Web.UI;

namespace Portal.UI.Controls
{
    public interface ITemplateFieldControl
    {
        Control GetInnerControl();
        Control GetLabelForDescription();
        Control GetLabelForTitleControl();
    }
}