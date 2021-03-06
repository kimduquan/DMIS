using System;
using ContentRepository.Storage.Schema;
using ContentRepository.Storage.Search.Internal;

namespace ContentRepository.Storage.Search
{
	public class StringExpression : Expression, IBinaryExpressionWrapper
	{
		private BinaryExpression _binExp;

		BinaryExpression IBinaryExpressionWrapper.BinExp
		{
			get { return _binExp; }
		}

		public StringExpression(PropertyType property, StringOperator op, string value)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (property.DataType != DataType.String)
				throw GetWrongPropertyDataTypeException("property", DataType.String);
            _binExp = new BinaryExpression(property, BinaryExpression.GetOperator(op), EscapeValue(value));
		}
		public StringExpression(PropertyType property, StringOperator op, PropertyType value)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (property.DataType != DataType.String)
				throw GetWrongPropertyDataTypeException("property", DataType.String);
			if (value == null)
				throw new ArgumentNullException("value");
			if (value.DataType != DataType.String)
				throw GetWrongPropertyDataTypeException("value", DataType.String);
			_binExp = new BinaryExpression(property, BinaryExpression.GetOperator(op), value);
		}
		public StringExpression(PropertyType property, StringOperator op, StringAttribute value)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (property.DataType != DataType.String)
				throw GetWrongPropertyDataTypeException("property", DataType.String);
			_binExp = new BinaryExpression(property, BinaryExpression.GetOperator(op), BinaryExpression.GetNodeAttribute(value));
		}
		public StringExpression(StringAttribute property, StringOperator op, string value)
		{
			_binExp = new BinaryExpression((NodeAttribute)property, BinaryExpression.GetOperator(op), EscapeValue(value));
		}
		public StringExpression(StringAttribute property, StringOperator op, PropertyType value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (value.DataType != DataType.String)
				throw GetWrongPropertyDataTypeException("value", DataType.String);
			_binExp = new BinaryExpression((NodeAttribute)property, BinaryExpression.GetOperator(op), value);
		}
		public StringExpression(StringAttribute property, StringOperator op, StringAttribute value)
		{
			_binExp = new BinaryExpression((NodeAttribute)property, BinaryExpression.GetOperator(op), BinaryExpression.GetNodeAttribute(value));
		}

		internal override void WriteXml(System.Xml.XmlWriter writer)
		{
            writer.WriteStartElement("String", NodeQuery.XmlNamespace);
			writer.WriteAttributeString("op", _binExp.Operator.ToString());
			_binExp.LeftValue.WriteXml(writer);
			_binExp.RightValue.WriteXml(writer);
			writer.WriteEndElement();
		}

        private string EscapeValue(string value)
        {
            if (value == null)
                return value;
            if (value.StartsWith("'") && value.EndsWith("'"))
                return value;
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return value;
            value = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
            if (!value.Contains(" "))
                return value;
            return String.Concat("\"", value, "\"");
        }
	}
}