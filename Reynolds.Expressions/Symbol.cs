using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions
{
	public class Symbol : Expression
	{
		bool isMatrix;
		public override bool IsMatrix
		{
			get
			{
				return isMatrix;
			}
		}

		public Symbol(string name, bool matrix = false)
		{
			Name = name;
			isMatrix = matrix;
		}

		public string Name
		{
			get;
			protected set;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		internal override Expression Derive(IDerivativeCache cache, Expression s)
		{
			if(isMatrix)
			{
				if(s == this)
					throw new NotImplementedException();  // MatrixExpression.Get(this.Rows, this.Columns, (i, j) => Expression.Indicator[i, j]) : Matrix.Get(
				else
					return MatrixExpression.Get(this.Rows, this.Columns, (i, j) => 0);
			}
			else
				return (s == this) ? 1 : 0;
		}

		public static ExpressionSubstitution operator |(Symbol symbol, Expression expression)
		{
			return new ExpressionSubstitution(symbol, expression);
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit(this.Name);
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit(this.Name);
		}

		//public override bool GetIsScalar(Expression[] arguments)
		//{
		//   return isScalar;
		//}
	}
}
