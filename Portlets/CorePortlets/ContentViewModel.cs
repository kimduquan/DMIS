using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository;
using System.Xml.Serialization;

namespace Portal.Portlets
{
    public class ContentViewModel
    {
        [XmlIgnore]
        public Content Content { get; internal set; }
    }
}
