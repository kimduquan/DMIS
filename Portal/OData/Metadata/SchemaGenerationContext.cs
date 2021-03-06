using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository.Schema;

namespace Portal.OData.Metadata
{
    internal class SchemaGenerationContext
    {
        public List<EntityType> EntityTypes = new List<EntityType>();
        public List<EnumType> EnumTypes = new List<EnumType>();
        public List<ComplexType> ComplexTypes = new List<ComplexType>();
        public List<Association> Associations = new List<Association>();

        public IEnumerable<FieldSetting> ListFieldSettings;
        public bool Flattening;
        public bool WithChildren;
    }
}
