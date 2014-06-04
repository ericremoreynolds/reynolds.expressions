using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions.Expressions
{
	internal class ApplyExpression : Expression
	{
		public Function f;
		public Expression[] x;

		public ApplyExpression(Function f, Expression[] x)
		{
			this.f = f;
			this.x = x;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			//var df = cache[f];
			Expression[] dx = x;
			for(int k = 0; k < x.Length; k++)
				if(x[k] != cache[x[k]])
					dx = new Expression[x.Length];
			if(dx != x)
				for(int k = 0; k < x.Length; k++)
					dx[k] = cache[x[k]];
			//if(f != df || x != dx)
			if(x != dx)
				return f[dx];
			else
				return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			List<Expression> terms = new List<Expression>();
			for(int k = 0; k < x.Length; k++)
			{
				var dx = cache[x[k]];
				if(!dx.IsZero)
					terms.Add(dx * f.PartialDerivative(k, x));
			}
			if(terms.Count == 0)
				return Expression.Zero;
			else if(terms.Count == 1)
				return terms[0];
			else
			{
				terms.Sort();
				return SumExpression.Get(terms.ToArray());
			}
		}

		protected override Expression Simplify(VisitCache cache)
		{
			//var df = cache[f];
			bool allConstant = true;
			Expression[] dx = new Expression[x.Length];
			for(int k = 0; k < x.Length; k++)
			{
				dx[k] = cache[x[k]];
				if(!dx[k].IsConstant)
					allConstant = false;
			}
			if(allConstant)
				return f.Evaluate((from dxx in dx
										 select dxx.Value).ToArray());
			else
			{
				var df = f.TrySimplify(dx);
				if(df != null)
					return cache[df];
				else
					return f[dx];
			}
		}

		public override string ToString()
		{
			return f.ToString(x);
		}

		public override string ToCode()
		{
			return f.ToCode(x);
		}
	}
}
