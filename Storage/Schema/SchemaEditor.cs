using System;
using System.Collections.Generic;
using ContentRepository.Storage.Data;
using ContentRepository.Storage.Caching.Dependency;
using Diagnostics;
using System.Diagnostics;

namespace ContentRepository.Storage.Schema
{
	public sealed class SchemaEditor : SchemaRoot
	{
		public SchemaEditor() { }

		public void Register()
		{
            SchemaEditor origSchema = new SchemaEditor();
			origSchema.Load();
            DataProvider.Current.AssertSchemaTimestampAndWriteModificationDate(this.SchemaTimestamp);
            SchemaWriter schemaWriter = DataProvider.Current.CreateSchemaWriter();
			RegisterSchema(origSchema, this, schemaWriter);
        }

		private static void RegisterSchema(SchemaEditor origSchema, SchemaEditor newSchema, SchemaWriter schemaWriter)
		{
            using (var traceOperation = Logger.TraceOperation("Write storage schema modifications."))
            {
                // Ensure transaction encapsulation
                bool isLocalTransaction = !TransactionScope.IsActive;
                if (isLocalTransaction)
                    TransactionScope.Begin();
                try
                {
                    List<PropertySet> modifiedPropertySets = new List<PropertySet>();
                    schemaWriter.Open();
                    WriteSchemaModifications(origSchema, newSchema, schemaWriter, modifiedPropertySets);
                    foreach (PropertySet modifiedPropertySet in modifiedPropertySets)
                    {
                        NodeTypeDependency.FireChanged(modifiedPropertySet.Id);
                    }
                    schemaWriter.Close();
                    if (isLocalTransaction)
                        TransactionScope.Commit();
                    ActiveSchema.Reset();
                    traceOperation.IsSuccessful = true;
                }
                finally
                {
                    IDisposable unmanagedWriter = schemaWriter as IDisposable;
                    if (unmanagedWriter != null)
                        unmanagedWriter.Dispose();
                    if (isLocalTransaction && TransactionScope.IsActive)
                        TransactionScope.Rollback();
                }
            }
		}
		private static void WriteSchemaModifications(SchemaEditor origSchema, SchemaEditor newSchema, SchemaWriter writer, List<PropertySet> modifiedPropertySets)
		{
			//---- #1: Delete types

			foreach (NodeType type in GetNodeTypeRootsToDelete(origSchema.NodeTypes, newSchema.NodeTypes))
				WriteDeleteNodeType(writer, type, origSchema, modifiedPropertySets);
            foreach (var type in GetTypesToDelete<ContentListType>(origSchema.ContentListTypes, newSchema.ContentListTypes))
                WriteDeleteContentListType(writer, type, origSchema, modifiedPropertySets);
			//foreach (PropertyType type in GetTypesToDelete<PropertyType>(origSchema.PropertyTypes, newSchema.PropertyTypes))
			//    WriteDeletePropertyType(writer, type);
			foreach (PermissionType type in GetTypesToDelete<PermissionType>(origSchema.PermissionTypes, newSchema.PermissionTypes))
				WriteDeletePermissionType(writer, type);

			//---- #2: Create or modify types

			WriteCreateOrModifyPropertyTypes(origSchema, newSchema, writer);
			WriteCreateOrModifyNodeTypes(origSchema, newSchema, modifiedPropertySets, writer);
            WriteCreateOrModifyContentListTypes(origSchema, newSchema, modifiedPropertySets, writer);
			WriteCreateOrModifyPermissionTypes(origSchema, newSchema, writer);

			//---- #3: Delete PropertyTypes
			foreach (PropertyType type in GetTypesToDelete<PropertyType>(origSchema.PropertyTypes, newSchema.PropertyTypes))
				WriteDeletePropertyType(writer, type);
		}

		//-------------------------------------------------------------- Remove commands

		private static void WriteDeletePropertyType(SchemaWriter writer, PropertyType propType)
		{
			writer.DeletePropertyType(propType);
		}
		private static void WriteDeleteNodeType(SchemaWriter writer, NodeType nodeType, SchemaEditor origSchema, List<PropertySet> modifiedPropertySets)
		{
			//-- recursive
			foreach (NodeType childType in nodeType.Children)
				WriteDeleteNodeType(writer, childType, origSchema, modifiedPropertySets);

			writer.DeleteNodeType(nodeType);
			if(!modifiedPropertySets.Contains(nodeType))
				modifiedPropertySets.Add(nodeType);
		}
        private static void WriteDeleteContentListType(SchemaWriter writer, ContentListType contentListType, SchemaEditor origSchema, List<PropertySet> modifiedPropertySets)
		{
			writer.DeleteContentListType(contentListType);
			if (!modifiedPropertySets.Contains(contentListType))
				modifiedPropertySets.Add(contentListType);
		}
		private static void WriteDeletePermissionType(SchemaWriter writer, PermissionType permType)
		{
			writer.DeletePermissionType(permType);
		}

		//-------------------------------------------------------------- CreateOrRemove commands

		private static void WriteCreateOrModifyPropertyTypes(SchemaEditor origSchema, SchemaEditor newSchema, SchemaWriter writer)
		{
			//-- new
			foreach (PropertyType newType in newSchema.PropertyTypes)
			{
				if(NeedToCreate<PropertyType>(origSchema.PropertyTypes, newType))
					writer.CreatePropertyType(newType.Name, newType.DataType, newType.Mapping, newType.IsContentListProperty);
			}
		}
		private static void WriteCreateOrModifyNodeTypes(SchemaEditor origSchema, SchemaEditor newSchema, List<PropertySet> modifiedPropertySets, SchemaWriter writer)
		{
			List<NodeType> _nodeTypesToEnumerate = new List<NodeType>();

			//-- collect only roots
			foreach (NodeType rootNodeType in newSchema.NodeTypes)
				if (rootNodeType.Parent == null)
					_nodeTypesToEnumerate.Add(rootNodeType);

			int index = 0;
			while (index < _nodeTypesToEnumerate.Count)
			{
				NodeType currentType = _nodeTypesToEnumerate[index++];
				NodeType origType = null;
				
				if (NeedToCreate<NodeType>(origSchema.NodeTypes, currentType))
				{
					writer.CreateNodeType(currentType.Parent, currentType.Name, currentType.ClassName);
				}
				else
				{
					origType = origSchema.NodeTypes[currentType.Name];
					string origParentName = origType.Parent == null ? null : origType.Parent.Name;
					string newParentName = currentType.Parent == null ? null : currentType.Parent.Name;
					bool parentChanged = origParentName != newParentName;
					if (parentChanged || origType.ClassName != currentType.ClassName)
					{
						writer.ModifyNodeType(origType, currentType.Parent, currentType.ClassName);
						if (!modifiedPropertySets.Contains(origType))
							modifiedPropertySets.Add(origType);
					}
				}

				//-- Property list (origType can be null)
				WriteAddOrRemovePropertyTypes(origType, currentType, modifiedPropertySets, writer);

				//-- Add children to enumerator
				_nodeTypesToEnumerate.AddRange(currentType.GetChildren());
			}
		}
        private static void WriteCreateOrModifyContentListTypes(SchemaEditor origSchema, SchemaEditor newSchema, List<PropertySet> modifiedPropertySets, SchemaWriter writer)
		{
			foreach (var newType in newSchema.ContentListTypes)
			{
				if (NeedToCreate<ContentListType>(origSchema.ContentListTypes, newType))
					writer.CreateContentListType(newType.Name);
				WriteAddOrRemovePropertyTypes(origSchema.ContentListTypes[newType.Name], newType, modifiedPropertySets, writer);
			}
		}
		private static void WriteCreateOrModifyPermissionTypes(SchemaEditor origSchema, SchemaEditor newSchema, SchemaWriter writer)
		{
			//-- new
			foreach (PermissionType newType in newSchema.PermissionTypes)
			{
				if (NeedToCreate<PermissionType>(origSchema.PermissionTypes, newType))
					writer.CreatePermissionType(newType.Name);
			}
		}

		private static void WriteAddOrRemovePropertyTypes(NodeType origSet, NodeType newSet, List<PropertySet> modifiedPropertySets, SchemaWriter writer)
		{
			if (origSet == null)
			{
				//-- New NodeType: add all property
				foreach (PropertyType propType in newSet.PropertyTypes)
					writer.AddPropertyTypeToPropertySet(propType, newSet, newSet.DeclaredPropertyTypes.Contains(propType));
				return;
			}
			bool origSetChanged = false;
			//-- Delete PropertyType if needed
			foreach (PropertyType propType in GetTypesToDelete<PropertyType>(origSet.PropertyTypes, newSet.PropertyTypes))
			{
				writer.RemovePropertyTypeFromPropertySet(propType, newSet);
				origSetChanged = true;
			}

			//-- Create or modify PropertyTypes
			foreach (PropertyType propType in newSet.PropertyTypes)
			{
				if (NeedToCreate<PropertyType>(origSet.PropertyTypes, propType))
				{
					bool isDeclared = newSet.DeclaredPropertyTypes.Contains(propType);
					writer.AddPropertyTypeToPropertySet(propType, newSet, isDeclared);
					origSetChanged = true;
				}
				else
				{
					//-- Modify Property declaration if needed (by the modifications in DeclaredPropertyTypes)
					bool newIsDeclared = newSet.DeclaredPropertyTypes.Contains(propType);
					bool origIsDeclared = origSet.DeclaredPropertyTypes[propType.Name] != null;
					if (newIsDeclared != origIsDeclared)
						writer.UpdatePropertyTypeDeclarationState(propType, newSet, newIsDeclared);
				}
			}

			if (origSetChanged && !modifiedPropertySets.Contains(origSet))
				modifiedPropertySets.Add(origSet);
		}
		private static void WriteAddOrRemovePropertyTypes(ContentListType origSet, ContentListType newSet, List<PropertySet> modifiedPropertySets, SchemaWriter writer)
		{
			bool origSetChanged = false;
			if (origSet == null)
			{
				//-- New NodeType: add all property
				foreach (PropertyType propType in newSet.PropertyTypes)
					writer.AddPropertyTypeToPropertySet(propType, newSet, true);
				return;
			}
			//-- Delete PropertyType if needed
			foreach (PropertyType propType in GetTypesToDelete<PropertyType>(origSet.PropertyTypes, newSet.PropertyTypes))
			{
				writer.RemovePropertyTypeFromPropertySet(propType, newSet);
				origSetChanged = true;
			}

			//-- Create or modify PropertyTypes
			foreach (PropertyType propType in newSet.PropertyTypes)
			{
				if (NeedToCreate<PropertyType>(origSet.PropertyTypes, propType))
				{
					writer.AddPropertyTypeToPropertySet(propType, newSet, true);
					origSetChanged = true;
				}
			}

			if (origSetChanged && !modifiedPropertySets.Contains(origSet))
				modifiedPropertySets.Add(origSet);
		}

		//============================================================== Tools

		private static List<T> GetTypesToDelete<T>(TypeCollection<T> origTypes, TypeCollection<T> newTypes) where T : SchemaItem
		{
			List<T> toDelete = new List<T>();
			foreach (T origType in origTypes)
			{
				T newType = newTypes[origType.Name];
				// Need to delete if newTypes does not contain this name or
				//    Id of new is 0 and Id of old is not 0.
				if (newType == null || (newType.Id == 0 && newType.Id != origType.Id))
					toDelete.Add(origType);
			}
			return toDelete;
		}
		private static bool NeedToCreate<T>(TypeCollection<T> origTypes, T newType) where T : SchemaItem
		{
			// Need to create if origTypes does not contain this name or
			//    Id of new is 0 and Id of old is not 0.
			T origType = origTypes[newType.Name];
			return origType == null || (newType.Id == 0 && origType.Id != 0);
		}
        //private static List<T> GetExclusiveTypesFromMain<T>(TypeCollection<T> mainSet, TypeCollection<T> subSet) where T : SchemaItem
        //{
        //    List<T> exclusiveTypes = new List<T>();
        //    foreach (T mainType in mainSet)
        //        if (subSet[mainType.Name] == null)
        //            exclusiveTypes.Add(mainType);
        //    return exclusiveTypes;
        //}
		private static List<NodeType> GetNodeTypeRootsToDelete(TypeCollection<NodeType> origSet, TypeCollection<NodeType> newSet)
		{
			//-- Walks (preorder) origSet NodeType tree and collects only deletable subtree roots.

			List<NodeType> _nodeTypeRootsToDelete = new List<NodeType>();
			List<NodeType> _nodeTypesToEnumerate = new List<NodeType>();

			//-- collect only roots
			foreach (NodeType rootNodeType in origSet)
				if (rootNodeType.Parent == null)
					_nodeTypesToEnumerate.Add(rootNodeType);

			int index = 0;
			while (index < _nodeTypesToEnumerate.Count)
			{
				NodeType currentType = _nodeTypesToEnumerate[index++];

				//-- ha nincs az ujban, akkor torolni kell, egyebkent a gyerekek mennek az enumeratorba
				//-- delete currentType if newSet does not contain it otherwise add its children to enumerator
				if (newSet.GetItemById(currentType.Id) == null)
					_nodeTypeRootsToDelete.Add(currentType);
				else
					_nodeTypesToEnumerate.AddRange(currentType.GetChildren());
			}

			return _nodeTypeRootsToDelete;
		}
	}
}