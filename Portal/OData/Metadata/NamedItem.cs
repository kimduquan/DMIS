using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Portal.OData.Metadata
{
    public abstract class NamedItem : SchemaItem
    {
        public string Name;
    }
}
