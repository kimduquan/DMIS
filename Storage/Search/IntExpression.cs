using System;
using ContentRepository.Storage.Schema;
using ContentRepository.Storage.Search.Internal;

namespace ContentRepository.Storage.Search
{
	public class IntExpression	: Expression, IBinaryExpressionWrapper
	{
		private BinaryExpression _binExp;

		BinaryExpression IBinaryExpressionWrapper.BinExp
		{
			get { return _binExp; }
		}

		public IntExpression(PropertyType property, ValueOperator op, int? value)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (property.DataType != DataType.Int)
				throw GetWrongPropertyDataTypeException("property", DataType.Int);
			_binExp = new BinaryExpression(property, BinaryExpression.GetOperator(op), value);
		}
		public IntExpression(PropertyType property, ValueOperator op, PropertyType value)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (property.DataType != DataType.Int)
				throw GetWrongPropertyDataTypeException("property", DataType.Int);
			if (value == null)
				throw new ArgumentNullException("value");
			if (value.DataType != DataType.Int)
				throw GetWrongPropertyDataTypeException("value", DataType.Int);
			_binExp = new BinaryExpression(property, BinaryExpression.GetOperator(op), value);
		}
		public IntExpression(PropertyType property, ValueOperator op, IntAttribute value)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (property.DataType != DataType.Int)
				throw GetWrongPropertyDataTypeException("property", DataType.Int);
			_binExp = new BinaryExpression(property, BinaryExpression.GetOperator(op), BinaryExpression.GetNodeAttribute(value));
		}
		public IntExpression(IntAttribute property, ValueOperator op, int? value)
		{
			_binExp = new BinaryExpression((NodeAttribute)property, BinaryExpression.GetOperator(op), value);
		}
		public IntExpression(IntAttribute property, ValueOperator op, PropertyType value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (value.DataType != DataType.Int)
				throw GetWrongPropertyDataTypeException("value", DataType.Int);
			_binExp = new BinaryExpression((NodeAttribute)property, BinaryExpression.GetOperator(op), value);
		}
		public IntExpression(IntAttribute property, ValueOperator op, IntAttribute value)
		{
			_binExp = new BinaryExpression((NodeAttribute)property, BinaryExpression.GetOperator(op), BinaryExpression.GetNodeAttribute(value));
		}

		internal override void WriteXml(System.Xml.XmlWriter writer)
		{
            writer.WriteStartElement("Int", NodeQuery.XmlNamespace);
			writer.WriteAttributeString("op", _binExp.Operator.ToString());
			_binExp.LeftValue.WriteXml(writer);
			//TODO: null?
			_binExp.RightValue.WriteXml(writer);
			writer.WriteEndElement();
		}
	}
}