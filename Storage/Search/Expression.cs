using System;
using System.Xml;
using System.Text;
using ContentRepository.Storage.Schema;

namespace ContentRepository.Storage.Search
{
	public abstract class Expression
	{
		public Expression Parent { get; internal set; }

		internal abstract void WriteXml(XmlWriter writer);
		public string ToXml()
		{
			StringBuilder sb = new StringBuilder();
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Encoding = Encoding.UTF8;
			settings.Indent = true;
            settings.OmitXmlDeclaration = true;
			XmlWriter writer = XmlWriter.Create(sb, settings);
			//writer.WriteStartDocument();
			writer.WriteStartElement("SearchExpression", NodeQuery.XmlNamespace);
			this.WriteXml(writer);
			writer.WriteEndElement();
			writer.Close();

			return sb.ToString();
		}

		protected static ArgumentException GetWrongPropertyDataTypeException(string paramName, DataType expectedDataType)
		{
			return new ArgumentException(String.Concat("The DataType of '", paramName, "' must be DataType.", expectedDataType.ToString()));
		}
	}
}