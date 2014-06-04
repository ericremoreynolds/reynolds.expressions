using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions.Expressions
{
	public class CoefficientExpression : Expression
	{
		public readonly double Coefficient;
		public readonly Expression Expression;

		//class Comparer : IEqualityComparer<CoefficientExpression>
		//{
		//   public bool Equals(CoefficientExpression x, CoefficientExpression y)
		//   {
		//      return x.Coefficient == y.Coefficient && x.Expression == y.Expression;
		//   }

		//   public int GetHashCode(CoefficientExpression obj)
		//   {
		//      return obj.GetHashCode();
		//   }
		//}

		static Dictionary<Tuple<double, Expression>, CoefficientExpression> cache = new Dictionary<Tuple<double, Expression>, CoefficientExpression>();
		public static Expression Get(double coefficient, Expression expression)
		{
			if(coefficient == 1.0)
				return expression;

			if(expression.IsConstant)
				return coefficient * expression.Value;

			var key = Tuple.Create(coefficient, expression);
			CoefficientExpression e;
			if(!cache.TryGetValue(key, out e))
				cache[key] = e = new CoefficientExpression(coefficient, expression);
			return e;
		}

		protected CoefficientExpression(double coefficient, Expression expression)
		{
			this.Coefficient = coefficient;
			this.Expression = expression;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			var ce = cache[this.Expression];
			if(ce != this.Expression)
				return Get(Coefficient, ce);
			else
				return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			var d = cache[this.Expression];
			if(d.IsZero)
				return d;
			else
				return Get(Coefficient, d);
		}

		protected override Expression Simplify(VisitCache cache)
		{
			if(Coefficient == 0.0)
				return Expression.Zero;

			var e = cache[this.Expression];
			if(e.IsConstant)
				return Coefficient * e.Value;

			CoefficientExpression ce = e as CoefficientExpression;
			if(ce != null)
				return Get(Coefficient * ce.Coefficient, ce.Expression);

			SumExpression se = e as SumExpression;
			if(se != null)
				return SumExpression.Get((from term in se.Terms
												  select Get(Coefficient, term)).ToArray());

			return Get(Coefficient, e);
		}

		public override string ToString()
		{
			if(Coefficient == -1.0)
				return "(-" + Expression.ToString() + ")";
			else
				return "(" + Coefficient.ToString() + " " + Expression.ToString() + ")";
		}

		public override string ToCode()
		{
			if(Coefficient == -1.0)
				return "(-" + Expression.ToCode() + ")";
			else
				return "(" + Coefficient.ToString() + "d*" + Expression.ToCode() + ")";
		}
	}
}
