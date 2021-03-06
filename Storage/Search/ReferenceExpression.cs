using ContentRepository.Storage.Schema;
using System.Globalization;
using System;
using ContentRepository.Storage.Search.Internal;

namespace ContentRepository.Storage.Search
{
	public class ReferenceExpression : Expression
	{
		//================================================================================= Fields

		private PropertyLiteral _referrerProperty;
		private Expression _expression;
		private Node _referencedNode;
		private bool _existenceOnly;

		//================================================================================= Properties

        public PropertyLiteral ReferrerProperty
		{
			get { return _referrerProperty; }
		}
        public Expression Expression
		{
			get { return _expression; }
		}
        public Node ReferencedNode
		{
			get { return _referencedNode; }
		}
        public bool ExistenceOnly
		{
			get { return _existenceOnly; }
		}
		
		//================================================================================= Construction

		public ReferenceExpression(PropertyType referrerProperty, Node referencedNode)
		{
			if (referrerProperty == null)
				throw new ArgumentNullException("referrerProperty");
			if (referrerProperty.DataType != DataType.Reference)
				throw new ArgumentOutOfRangeException("referrerProperty", "The DataType of 'referrerProperty' must be Reference");
			_referrerProperty = new PropertyLiteral(referrerProperty);
			_referencedNode = referencedNode;
			//_existenceOnly = false;
		}
		public ReferenceExpression(PropertyType referrerProperty, Expression expression)
		{
			if (referrerProperty == null)
				throw new ArgumentNullException("referrerProperty");
			if (referrerProperty.DataType != DataType.Reference)
				throw new ArgumentOutOfRangeException("referrerProperty", "The DataType of ''referrerProperty'' must be Reference");
			if (expression == null)
				throw new ArgumentNullException("expression");
			_referrerProperty = new PropertyLiteral(referrerProperty);
			_expression = expression;
			//_existenceOnly = false;
		}
		public ReferenceExpression(PropertyType referrerProperty)
		{
			if (referrerProperty == null)
				throw new ArgumentNullException("referrerProperty");
			if (referrerProperty.DataType != DataType.Reference)
				throw new ArgumentOutOfRangeException("referrerProperty", "The DataType of 'referrerProperty' must be Reference");
			_referrerProperty = new PropertyLiteral(referrerProperty);
			_existenceOnly = true;
		}
		public ReferenceExpression(ReferenceAttribute referrerProperty, Node referencedNode)
		{
			_referrerProperty = new PropertyLiteral((NodeAttribute)referrerProperty);
			_referencedNode = referencedNode;
			//_existenceOnly = false;
		}
		public ReferenceExpression(ReferenceAttribute referrerProperty, Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");
			_referrerProperty = new PropertyLiteral((NodeAttribute)referrerProperty);
			_expression = expression;
			//_existenceOnly = false;
		}
		public ReferenceExpression(ReferenceAttribute referrerProperty)
		{
			_referrerProperty = new PropertyLiteral((NodeAttribute)referrerProperty);
			_existenceOnly = true;
		}

		//================================================================================= Methods

		internal override void WriteXml(System.Xml.XmlWriter writer)
		{
            writer.WriteStartElement("Reference", NodeQuery.XmlNamespace);

			_referrerProperty.WriteXml(writer);
			if (_existenceOnly)
				writer.WriteAttributeString("existenceOnly", "yes");
			if(_referencedNode != null)
				writer.WriteAttributeString("referencedNodeId", _referencedNode.Id.ToString(CultureInfo.InvariantCulture));
			if (_expression != null)
				_expression.WriteXml(writer);

			writer.WriteEndElement();
		}
	}
}