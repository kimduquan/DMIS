
using Portal.UI.PortletFramework;
namespace Portal.Portlets
{
    public class SiteMenu : SiteMenuBase
    {
        public SiteMenu()
        {
            this.Name = "$SiteMenu:PortletDisplayName";
            this.Description = "$SiteMenu:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.Navigation);
        }
    }
}
