using System;
using ContentRepository.Storage.Schema;
using ContentRepository.Storage.Search;
using ContentRepository.Storage.Search.Internal;

namespace ContentRepository.Storage.Search
{
	public class CurrencyExpression : Expression, IBinaryExpressionWrapper
	{
		private BinaryExpression _binExp;

		BinaryExpression IBinaryExpressionWrapper.BinExp
		{
			get { return _binExp; }
		}

		//-- There are not any NodeAttribute with Currency datatype

		public CurrencyExpression(PropertyType property, ValueOperator op, decimal? value)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (property.DataType != DataType.Currency)
				throw GetWrongPropertyDataTypeException("property", DataType.Currency);
			_binExp = new BinaryExpression(property, BinaryExpression.GetOperator(op), value);
		}
		public CurrencyExpression(PropertyType property, ValueOperator op, PropertyType value)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (property.DataType != DataType.Currency)
				throw GetWrongPropertyDataTypeException("value", DataType.Currency);
			if (value == null)
				throw new ArgumentNullException("value");
			if (value.DataType != DataType.Currency)
				throw GetWrongPropertyDataTypeException("value", DataType.Currency);
			_binExp = new BinaryExpression(property, BinaryExpression.GetOperator(op), value);
		}

		internal override void WriteXml(System.Xml.XmlWriter writer)
		{
            writer.WriteStartElement("Currency", NodeQuery.XmlNamespace);
			writer.WriteAttributeString("op", _binExp.Operator.ToString());
			_binExp.LeftValue.WriteXml(writer);
			//TODO: null?
			_binExp.RightValue.WriteXml(writer);
			writer.WriteEndElement();
		}
	}
}