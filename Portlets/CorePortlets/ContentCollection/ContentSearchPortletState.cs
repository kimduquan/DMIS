using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Portal.UI.PortletFramework;

namespace Portal.Portlets.ContentCollection
{
    [Serializable]
    public class ContentSearchPortletState : ContentCollectionPortletState
    {
        public ContentSearchPortletState(PortletBase portlet)
            : base(portlet)
        {

        }

        public string ExportQueryFields { get; set; }
    }
}
