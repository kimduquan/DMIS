using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Portal.OData.Metadata
{
    public class NavigationProperty : NamedItem
    {
        public string Relationship;
        public string FromRole;
        public string ToRole;
        //public ???? ContainsTarget      //later

        public override void WriteXml(TextWriter writer)
        {
            writer.Write("        <NavigationProperty");
            WriteAttribute(writer, "Name", Name);
            WriteAttribute(writer, "Relationship", Relationship);
            WriteAttribute(writer, "FromRole", FromRole);
            WriteAttribute(writer, "ToRole", ToRole);
            writer.WriteLine("/>");
        }
    }
}
