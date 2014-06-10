using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Expressions.Expressions;

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
			return -Expression.Sin[x[0]];
		}

		public override string ToString()
		{
			return "cos";
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit("Math.Cos");
		}
	}

	internal class SinFunction : FunctionExpression
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
			return Math.Sin(x[0]);
		}

		public override Expression GetPartialDerivative(int i, params Expression[] x)
		{
			return Expression.Cos[x[0]];
		}

		public override string ToString()
		{
			return "sin";
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit("Math.Sin");
		}
	}

	internal class ExpFunction : FunctionExpression
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
			return Math.Exp(x[0]);
		}

		public override Expression GetPartialDerivative(int i, params Expression[] x)
		{
			return this[x];
		}

		public override string ToString()
		{
			return "exp";
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit("Math.Exp");
		}
	}

	internal class LogFunction : FunctionExpression
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
			return Math.Log(x[0]);
		}

		public override Expression GetPartialDerivative(int i, params Expression[] x)
		{
			return 1 / x[0];
		}

		public override string ToString()
		{
			return "log";
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit("Math.Log");
		}
	}

	internal class PowFunction : FunctionExpression
	{
		public override int Arity
		{
			get
			{
				return 2;
			}
		}

		public override double Evaluate(params double[] x)
		{
			return Math.Pow(x[0], x[1]);
		}

		public override Expression GetPartialDerivative(int i, params Expression[] x)
		{
			if(i == 0)
				return x[1] * Expression.Pow[x[0], x[1] - 1];
			else
				return Expression.Log[x[0]] * Expression.Pow[x[0], x[1]];
		}

		public override void GenerateCode(ICodeGenerationContext context, Expression[] x)
		{
			if(x[1].IsConstant && x[1].Value == -1)
				context.Emit("(1d/").Emit(x[0]).Emit(")");
			else
				base.GenerateCode(context, x);
		}

		protected override Expression Normalize(INormalizeContext context, Expression[] arguments)
		{
			ApplicationExpression ae;
			ProductExpression pe;
			CoefficientExpression ce;
			if(null != (pe = arguments[0] as ProductExpression))
				return context.Normalize(ProductExpression.Get((from f in pe.Factors select Expression.Pow[f, arguments[1]]).ToArray()));
			else if(null != (ce = arguments[0] as CoefficientExpression))
				return context.Normalize(ProductExpression.Get(Expression.Pow[ce.Coefficient, arguments[1]], Expression.Pow[ce.Expression, arguments[1]]));
			else if(null != (ae = arguments[0] as ApplicationExpression) && ae.Applicand == Expression.Pow)
				return context.Normalize(Expression.Pow[ae.Arguments[0], ae.Arguments[1] * arguments[1]]);
			else
				return base.Normalize(context, arguments);
		}

		public override string ToString(Expression[] x)
		{
			return x[0].ToString() + "^" + x[1].ToString();
		}

		public override string ToString()
		{
			return "pow";
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit("Math.Pow");
		}
	}
}
