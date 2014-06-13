using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Mappings;

namespace Reynolds.Expressions.Expressions
{
	public class CoefficientExpression : Expression
	{
		public readonly dynamic Coefficient;
		public readonly Expression Expression;

		static WeakLazyMapping<object, Expression, CoefficientExpression> cache = new WeakLazyMapping<object, Expression, CoefficientExpression>((o, e) => new CoefficientExpression(o, e));
		public static Expression Get(dynamic coefficient, Expression expression)
		{
			CoefficientExpression ce = expression as CoefficientExpression;
			if(ce != null)
			{
				coefficient = coefficient * ce.Coefficient;
				expression = ce.Expression;
			}

			if(coefficient == 1)
				return expression;

			if(expression.IsConstant)
				return coefficient * expression.Value;

			return cache[(object) coefficient, expression];
		}

		protected CoefficientExpression(object coefficient, Expression expression)
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

		protected override Expression Normalize(INormalizeContext context)
		{
			if(Coefficient == 0)
				return Expression.Constant(Coefficient);

			var e = context.Normalize(this.Expression);
			if(e.IsConstant)
				return Coefficient * e.Value;

			CoefficientExpression ce = e as CoefficientExpression;
			if(ce != null)
				return Get(Coefficient * ce.Coefficient, ce.Expression);

			SumExpression se = e as SumExpression;
			if(se != null)
				return SumExpression.Get((from term in se.Terms select Get((object) Coefficient, term)).ToArray());

			return Get(Coefficient, e);
		}

		public override void ToString(IStringifyContext context)
		{
			if(context.EnclosingOperator > StringifyOperator.Product)
				context.Emit("(");
			if(Coefficient == -1)
				context.Emit("-");
			else
				context.Emit(Coefficient);
			context.Emit(Expression, StringifyOperator.Product);
			if(context.EnclosingOperator > StringifyOperator.Product)
				context.Emit(")");
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			if(Coefficient == -1)
				context.Emit("(-").Emit(Expression).Emit(")");
			else
				context.Emit("(").Emit(Coefficient).Emit("d*").Emit(Expression).Emit(")");
		}
	}
}
