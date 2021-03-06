using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;
using System.Xml;

namespace  ContentRepository.Schema
{
	internal class FieldDescriptor
	{
		public ContentType Owner { get; private set; }
		internal string FieldName { get; private set; }
		internal string FieldTypeShortName { get; private set; }
		internal string FieldTypeName { get; private set; }
        internal string DisplayName { get; private set; }
		internal string Description { get; private set; }
		internal string Icon { get; private set; }
        internal List<string> Bindings { get; private set; }
        internal string IndexingMode { get; private set; }
        internal string IndexStoringMode { get; private set; }
        internal string IndexingTermVector { get; private set; }
        internal string Analyzer { get; private set; }
        internal string IndexHandlerTypeName { get; private set; }
        internal string FieldSettingTypeName { get; private set; }
		public XPathNavigator ConfigurationElement { get; private set; }
		public XPathNavigator AppInfo { get; private set; }
		public IXmlNamespaceResolver XmlNamespaceResolver { get; private set; }
		public RepositoryDataType[] DataTypes { get; private set; }
        public bool IsContentListField { get; private set; }

		private FieldDescriptor() { }

		internal static FieldDescriptor Parse(XPathNavigator fieldElement, IXmlNamespaceResolver nsres, ContentType contentType)
		{
			FieldDescriptor fdesc = new FieldDescriptor();
			fdesc.Owner = contentType;
            var fieldName = fieldElement.GetAttribute("name", String.Empty);
            fdesc.FieldName = fieldName;
			fdesc.FieldTypeShortName = fieldElement.GetAttribute("type", String.Empty);
			fdesc.FieldTypeName = fieldElement.GetAttribute("handler", String.Empty);
            fdesc.IsContentListField = fdesc.FieldName[0] == '#';
            if (String.IsNullOrEmpty(fdesc.FieldTypeShortName))
                fdesc.FieldTypeShortName = FieldManager.GetShortName(fdesc.FieldTypeName);

			if (fdesc.FieldTypeName.Length == 0)
			{
				if (fdesc.FieldTypeShortName.Length == 0)
					throw new ContentRegistrationException("Field element's 'handler' attribute is required if 'type' attribute is not given.", contentType.Name, fdesc.FieldName);
				fdesc.FieldTypeName = FieldManager.GetFieldHandlerName(fdesc.FieldTypeShortName);
			}

			fdesc.Bindings = new List<string>();

			foreach (XPathNavigator subElement in fieldElement.SelectChildren(XPathNodeType.Element))
			{
				switch (subElement.LocalName)
				{
                    case "DisplayName":
                        fdesc.DisplayName = subElement.Value;
						break;
					case "Description":
						fdesc.Description = subElement.Value;
						break;
					case "Icon":
						fdesc.Icon = subElement.Value;
						break;
					case "Bind":
						fdesc.Bindings.Add(subElement.GetAttribute("property", String.Empty));
						break;
                    case "Indexing":
                        foreach (XPathNavigator indexingSubElement in subElement.SelectChildren(XPathNodeType.Element))
                        {
                            switch (indexingSubElement.LocalName)
                            {
                                case "Mode": fdesc.IndexingMode = indexingSubElement.Value; break;
                                case "Store": fdesc.IndexStoringMode = indexingSubElement.Value; break;
                                case "TermVector": fdesc.IndexingTermVector = indexingSubElement.Value; break;
                                case "Analyzer": fdesc.Analyzer = indexingSubElement.Value; break;
                                case "IndexHandler": fdesc.IndexHandlerTypeName = indexingSubElement.Value; break;
                            }
                        }
                        break;
					case "Configuration":
						fdesc.ConfigurationElement = subElement;
						fdesc.FieldSettingTypeName = subElement.GetAttribute("handler", String.Empty);
						break;
					case "AppInfo":
                        fdesc.AppInfo = subElement;
						break;
					default:
						throw new NotSupportedException(String.Concat("Unknown element in Field: ", subElement.LocalName));
				}
			}

			//-- Default binding;
			RepositoryDataType[] dataTypes = FieldManager.GetDataTypes(fdesc.FieldTypeShortName);
			fdesc.DataTypes = dataTypes;
            if (fdesc.IsContentListField)
            {
                foreach(var d in dataTypes)
                    fdesc.Bindings.Add(null);
            }
            else
            {
                if (dataTypes.Length > 1 && fdesc.Bindings.Count != dataTypes.Length)
                    throw new ContentRegistrationException("Missing excplicit 'Binding' elements", contentType.Name, fdesc.FieldName);
                if (dataTypes.Length == 1 && fdesc.Bindings.Count == 0)
                    fdesc.Bindings.Add(fdesc.FieldName);
            }

			fdesc.XmlNamespaceResolver = nsres;

			return fdesc;
		}
	}
}