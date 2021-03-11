using System.Web.UI;

namespace Portal.UI.Controls
{
    internal abstract class DefaultButtonsFactoryBase
    {
        public Control CurrentControl { get; set; }
        public abstract Control CreateActionButtons(DefaultButtonType button);
    }
}
