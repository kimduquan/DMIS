using System.Collections.Generic;
using System;

namespace ContentRepository.Storage.Search
{
	public class ExpressionList : Expression
	{
		private ChainOperator _operatorType;
		private List<Expression> _expressions;

		public ChainOperator OperatorType
		{
			get { return _operatorType; }
		}
		public List<Expression> Expressions
		{
			get { return _expressions; }
		} 

		public ExpressionList(ChainOperator operatorType)
		{
			_operatorType = operatorType;
			_expressions = new List<Expression>();
		}
		public ExpressionList(ChainOperator operatorType, Expression expression) : this(operatorType)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");
			Add(expression);
		}
		public ExpressionList(ChainOperator operatorType, Expression exp0, Expression exp1) : this(operatorType)
		{
			if (exp0 == null)
				throw new ArgumentNullException("exp0");
			if (exp1 == null)
				throw new ArgumentNullException("exp1");
			Add(exp0);
			Add(exp1);
		}
		public ExpressionList(ChainOperator operatorType, Expression exp0, Expression exp1, Expression exp2) : this(operatorType)
		{
			if (exp0 == null)
				throw new ArgumentNullException("exp0");
			if (exp1 == null)
				throw new ArgumentNullException("exp1");
			if (exp2 == null)
				throw new ArgumentNullException("exp2");
			Add(exp0);
			Add(exp1);
			Add(exp2);
		}
		public ExpressionList(ChainOperator operatorType, params Expression[] expressions) : this(operatorType)
		{
			foreach (Expression exp in expressions)
				if (exp == null)
					throw new ArgumentOutOfRangeException("expressions", "Expression parameter cannot be null");
            foreach (var expression in expressions)
                Add(expression);
		}

		public void Add(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");
			_expressions.Add(expression);
			expression.Parent = this;
		}

		internal override void WriteXml(System.Xml.XmlWriter writer)
		{
            writer.WriteStartElement(_operatorType.ToString(), NodeQuery.XmlNamespace);
			foreach (Expression exp in _expressions)
				exp.WriteXml(writer);
			writer.WriteEndElement();
		}
	}
}