using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions.Functions
{
	internal class CosFunction : FunctionExpression
	{
		public override int Arity
		{
			get
			{
				return 1;
			}
		}

		public override double Evaluate(params double[] x)
		{
			return Math.Cos(x[0]);
		}

		public override Expression GetPartialDerivative(int i, params Expression[] x)
		{
			return Expression.Sin[x[0]];
		}
	}
}
