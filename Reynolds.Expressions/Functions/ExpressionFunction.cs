using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions.Functions
{
	public class ExpressionFunction : FunctionExpression
	{
		public readonly Symbol[] Arguments;
		public readonly Expression Body;

		public ExpressionFunction(Symbol[] arguments, Expression body)
		{
			Arguments = arguments;
			Body = body;
			//partials = new ExpressionFunction[arguments.Length];
			//for(int k = 0; k < arguments.Length; k++)
			//   partials[k] = new ExpressionFunction(arguments, body.Derive(arguments[k]).Simplify());
		}

		public override int Arity
		{
			get
			{
				return Arguments.Length;
			}
		}

		public override double Evaluate(params double[] x)
		{
			throw new NotImplementedException();
		}

		public override Expression GetPartialDerivative(int i, params Expression[] x)
		{
			throw new NotImplementedException();
		}

		public override Expression this[params Expression[] x]
		{
			get
			{
				ExpressionSubstitution[] subs = new ExpressionSubstitution[Arguments.Length];
				for(int k = 0; k < Arguments.Length; k++)
					subs[k] = Arguments[k] | x[k];
				return Body.Substitute(subs);
			}
		}

		public override string ToString(Expression[] x)
		{
			return "((" + string.Join(", ", from a in Arguments
													  select a.ToString()) + ") => " + Body.ToString() + ")";
		}

		public override string ToCode(Expression[] x)
		{
			return "((" + string.Join(", ", from a in Arguments
													  select a.ToString()) + ") => " + Body.ToCode() + ")";
		}
	}
}
