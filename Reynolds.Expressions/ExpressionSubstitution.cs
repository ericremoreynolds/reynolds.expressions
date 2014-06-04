using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions
{
	public class ExpressionSubstitution
	{
		public ExpressionSubstitution(Expression expression, Expression substitute)
		{
			this.Expression = expression;
			this.Substitute = substitute;
		}

		public Expression Expression
		{
			get;
			protected set;
		}

		public Expression Substitute
		{
			get;
			protected set;
		}
	}
}
