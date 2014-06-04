using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Expressions.Expressions;
using Reynolds.Mappings;

namespace Reynolds.Expressions
{
	public abstract class Function
	{
		public abstract int Arity
		{
			get;
		}

		//public Function Create(Symbol[] arguments, Expression body)
		//{
		//   return new ExpressionFunction(arguments, body);
		//}

		public abstract double Evaluate(params double[] x);

		public abstract Expression PartialDerivative(int i, params Expression[] x);

		static Dictionary<Function, Dictionary<Expression[], Expression>> applyExpressions = new Dictionary<Function, Dictionary<Expression[], Expression>>();

		public virtual Expression this[params Expression[] x]
		{
			get
			{
				if(x.Length != Arity)
					throw new Exception("Wrong number of arguments.");

				Dictionary<Expression[], Expression> d;
				if(!applyExpressions.TryGetValue(this, out d))
					applyExpressions[this] = d = new Dictionary<Expression[], Expression>(ReferenceTypeArrayEqualityComparer<Expression>.Instance);
				Expression e;
				if(!d.TryGetValue(x, out e))
					d[x] = e = new ApplyExpression(this, x);
				return e;
			}
		}

		public virtual Expression TrySimplify(params Expression[] x)
		{
			return null; //	this[x];
		}

		public abstract string ToString(Expression[] x);

		public abstract string ToCode(Expression[] x);
	}
}
