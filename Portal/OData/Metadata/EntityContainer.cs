using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Portal.OData.Metadata
{
    public class EntityContainer : NamedItem
    {
        public List<EntitySet> EntitySets;
        public List<AssociationSet> AssociationSets;
        public List<FunctionImport> FunctionImports;

        public override void WriteXml(TextWriter writer)
        {
            //      <EntityContainer Name="DemoService" m:IsDefaultEntityContainer="true">
            writer.Write("      <EntityContainer");
            WriteAttribute(writer, "Name", Name);
            WriteAttribute(writer, "m:IsDefaultEntityContainer", "true");
            writer.WriteLine(">");

            WriteCollectionXml(writer, EntitySets);
            WriteCollectionXml(writer, AssociationSets);
            WriteCollectionXml(writer, FunctionImports);

            writer.Write("      </EntityContainer>");
        }
    }
}
