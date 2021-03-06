using System;
using System.Collections.Generic;
using System.Text;
using ContentRepository.Storage.Data;

namespace ContentRepository.Storage.Schema
{
	public sealed class PropertyType : SchemaItem
	{
		//================================================================================ Fields

		private DataType _dataType;
		private int _mapping;
		private bool _isContentListProperty;

		//================================================================================ Properties

		public DataType DataType
		{
			get { return _dataType; }
		}
		public int Mapping
		{
			get { return _mapping; }
		}
		public bool IsContentListProperty
		{
			get { return _isContentListProperty; }
		}
		public object DefaultValue
		{
			get { return GetDefaultValue(this.DataType); }
		}
		public static object GetDefaultValue(DataType dataType)
		{
			var compatible = RepositoryConfiguration.BackwardCompatibilityDefaultValues;
			switch (dataType)
			{
				case DataType.Int:
					return default(int);
				case DataType.Currency:
					return default(decimal);
				case DataType.DateTime:
					return compatible ? DateTime.UtcNow : default(DateTime);
				case DataType.String:
				case DataType.Text:
					return compatible ? string.Empty : default(string);
				default:
					return null;
			}
		}

		//================================================================================ Construction

		internal PropertyType(ISchemaRoot schemaRoot, string name, int id, DataType dataType, int mapping, bool isContentListProperty)
			: base(schemaRoot, name, id)
		{
			_dataType = dataType;
			_mapping = mapping;
			_isContentListProperty = isContentListProperty;
			if (!isContentListProperty)
				CheckMapping();
		}

		//================================================================================ Static Methods

		public static PropertyType GetByName(string propertyTypeName)
		{
			return NodeTypeManager.Current.PropertyTypes[propertyTypeName];
		}

		//================================================================================ Methods

        public PropertyMapping GetDatabaseInfo()
        {
            return DataProvider.GetPropertyMapping(this);
        }

		internal void CheckPropertyTypeUsage(string errorMessage)
		{
			foreach (NodeType nodeType in this.SchemaRoot.NodeTypes)
				if (nodeType.PropertyTypes.Contains(this))
					throw new SchemaEditorCommandException(errorMessage);
		}

		private void CheckMapping()
		{
			foreach (PropertyType slot in this.SchemaRoot.PropertyTypes)
				if (slot.DataType == _dataType && slot.Mapping == _mapping)
					throw new InvalidSchemaException(String.Concat(SR.Exceptions.Schema.Msg_MappingAlreadyExists, ": name=", this.Name, ", dataType", _dataType, ", mapping", _mapping));
		}

	}
}