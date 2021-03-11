using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Portal.PortletFramework
{
    public interface IContentProvider
    {
        string ContentTypeName { get; set; }
        string ContentName { get; set; }
    }
}
