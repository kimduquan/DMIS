using System;
using System.Collections.Generic;
using System.Text;

namespace ContentRepository.Storage.Schema
{
	internal interface ISchemaRoot
	{
		TypeCollection<PropertyType> PropertyTypes { get;}
		TypeCollection<NodeType> NodeTypes { get;}
		TypeCollection<ContentListType> ContentListTypes { get;}
		TypeCollection<PermissionType> PermissionTypes { get;}

		void Clear();
		void Load();
	}
}