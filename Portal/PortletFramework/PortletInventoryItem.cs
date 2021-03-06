using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository.Storage;
using System.Reflection;
using ContentRepository;

namespace Portal.UI.PortletFramework
{
    public class PortletInventoryItem
    {
        public PortletBase Portlet { get; set; }
        public System.IO.Stream ImageStream { get; set; }

        public ContentRepository.Fields.ImageField.ImageFieldData GetImageFieldData(Field field)
        {
            if (this.ImageStream == null)
                return null;

            var binaryData = new BinaryData();
            binaryData.SetStream(this.ImageStream);
            return new ContentRepository.Fields.ImageField.ImageFieldData(field, null, binaryData);
        }
        public static PortletInventoryItem Create(PortletBase portlet, Assembly assembly)
        {
            var portletItem = new PortletInventoryItem();
            portletItem.Portlet = portlet;

            // get resource image
            var imageName = string.Concat(portlet.GetType().ToString(), ".png");
            var imageStream = assembly.GetManifestResourceStream(imageName);
            if (imageStream != null)
                portletItem.ImageStream = imageStream;

            return portletItem;
        }
    }
}
