using System;
using ContentRepository.Storage.Schema;
using ContentRepository.Storage.Search.Internal;

namespace ContentRepository.Storage.Search
{
	public class DateTimeExpression	: Expression, IBinaryExpressionWrapper
	{
		private BinaryExpression _binExp;

		BinaryExpression IBinaryExpressionWrapper.BinExp
		{
			get { return _binExp; }
		}

		public DateTimeExpression(PropertyType property, ValueOperator op, DateTime? value)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (property.DataType != DataType.DateTime)
				throw GetWrongPropertyDataTypeException("property", DataType.DateTime);
			_binExp = new BinaryExpression(property, BinaryExpression.GetOperator(op), value);
		}
		public DateTimeExpression(PropertyType property, ValueOperator op, PropertyType value)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (property.DataType != DataType.DateTime)
				throw GetWrongPropertyDataTypeException("property", DataType.DateTime);
			if (value == null)
				throw new ArgumentNullException("value");
			if (value.DataType != DataType.DateTime)
				throw GetWrongPropertyDataTypeException("value", DataType.DateTime);
			_binExp = new BinaryExpression(property, BinaryExpression.GetOperator(op), value);
		}
		public DateTimeExpression(PropertyType property, ValueOperator op, DateTimeAttribute value)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (property.DataType != DataType.DateTime)
				throw new ArgumentOutOfRangeException("property", "The DataType of 'property' must be DataType.DateTime");
			_binExp = new BinaryExpression(property, BinaryExpression.GetOperator(op), BinaryExpression.GetNodeAttribute(value));
		}
		public DateTimeExpression(DateTimeAttribute property, ValueOperator op, DateTime? value)
		{
			_binExp = new BinaryExpression((NodeAttribute)property, BinaryExpression.GetOperator(op), value);
		}
		public DateTimeExpression(DateTimeAttribute property, ValueOperator op, PropertyType value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (value.DataType != DataType.DateTime)
				throw GetWrongPropertyDataTypeException("value", DataType.DateTime);
			_binExp = new BinaryExpression((NodeAttribute)property, BinaryExpression.GetOperator(op), value);
		}
		public DateTimeExpression(DateTimeAttribute property, ValueOperator op, DateTimeAttribute value)
		{
			_binExp = new BinaryExpression((NodeAttribute)property, BinaryExpression.GetOperator(op), BinaryExpression.GetNodeAttribute(value));
		}

		internal override void WriteXml(System.Xml.XmlWriter writer)
		{
            writer.WriteStartElement("DateTime", NodeQuery.XmlNamespace);
			writer.WriteAttributeString("op", _binExp.Operator.ToString());
			_binExp.LeftValue.WriteXml(writer);
			_binExp.RightValue.WriteXml(writer);
			writer.WriteEndElement();
		}
	}
}