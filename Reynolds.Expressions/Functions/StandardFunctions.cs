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

		protected override Expression GetPartialDerivative(int i, Expression[] x)
		{
			return -Expression.Sin[x[0]];
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit("cos");
		}

		public override void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
		{
			if(arguments.Length == 0)
				context.Emit("Math.Cos");
			else
				base.GenerateCode(context, arguments);
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

		protected override Expression GetPartialDerivative(int i, Expression[] x)
		{
			return Expression.Cos[x[0]];
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit("sin");
		}

		public override void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
		{
			if(arguments.Length == 0)
				context.Emit("Math.Sin");
			else
				base.GenerateCode(context, arguments);
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

		protected override Expression GetPartialDerivative(int i, Expression[] x)
		{
			return this[x];
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit("exp");
		}

		public override void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
		{
			if(arguments.Length == 0)
				context.Emit("Math.Exp");
			else
				base.GenerateCode(context, arguments);
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

		protected override Expression GetPartialDerivative(int i, Expression[] x)
		{
			return 1 / x[0];
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit("log");
		}
		
		public override void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
		{
			if(arguments.Length == 0)
				context.Emit("Math.Log");
			else
				base.GenerateCode(context, arguments);
		}
	}

	public class PowFunction : FunctionExpression
	{
		protected PowFunction()
		{
			Domain = PowFunctionDomain.Instance;
		}

		public readonly static PowFunction Instance = new PowFunction();

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

		protected override Expression GetPartialDerivative(int i, Expression[] x)
		{
			if(i == 0)
				return x[1] * Expression.Pow[x[0], x[1] - 1];
			else
				return Expression.Log[x[0]] * Expression.Pow[x[0], x[1]];
		}

		public override void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
		{
			if(arguments.Length == 0)
				context.Emit("Math.Cos");
			else if(arguments[1].IsConstant && arguments[1].Value == -1)
				context.Emit("(1d/").Emit(arguments[0]).Emit(")");
			else
				base.GenerateCode(context, arguments);
		}

		//public override Expression Normalize(Expression[] arguments)
		//{
		//   ApplicationExpression ae;
		//   ProductExpression pe;
		//   if(null != (pe = arguments[0] as ProductExpression))
		//      return ProductExpression.Get((from f in pe.Factors select Expression.Pow[f, arguments[1]]).ToArray());
		//   else if(null != (ae = arguments[0] as ApplicationExpression) && ae.Target == Expression.Pow)
		//      return Expression.Pow[ae.Argument[0], ae.Argument[1] * arguments[1]];
		//   else
		//      return base.Normalize(arguments);
		//}

		public override Expression Apply(params Expression[] arguments)
		{
			if(arguments.Length == 1)
			{
				var ae = arguments[0].GetElement(0) as ApplicationExpression;
				if(null != ae)
				{
					// Pow[a * b, n] ==> Pow[a, n] * Pow[b, n] when b in Reals
					if(ae.Argument.Domain <= Domain.Reals)
						return Expression.Pow[ae.Target, arguments[0].GetElement(1)] * Expression.Pow[ae.Argument, arguments[0].GetElement(0)];
					// Pow[Pow[x, a], b] ==> Pow[x, a*b]
					else if(ae.Target == Expression.Pow)
						return Expression.Pow[ae.Argument.GetElement(0), ae.Argument.GetElement(1) * arguments[0].GetElement(1)];
				}
				else if(arguments[0].GetElement(1).IsConstant && arguments[0].GetElement(1).Value == 1)
					return arguments[0].GetElement(0);
			}
			else if(arguments.Length == 2)
			{
				ApplicationExpression ae;
				// Pow[x, n][x] ==> Pow[x, n+1]
				if(arguments[0].GetElement(0) == arguments[1])
					return Expression.Pow[arguments[1], arguments[0].GetElement(1) + 1];
				// Pow[x, n][Pow[x, m]] ==> Pow[x, n+m]
				else if(null != (ae = arguments[1] as ApplicationExpression) && ae.Target == Expression.Pow && ae.Argument.GetElement(0) == arguments[0].GetElement(0))
					return Expression.Pow[arguments[0].GetElement(0), arguments[0].GetElement(1) + ae.Argument.GetElement(1)];
			}

			return base.Apply(arguments);
		}

		public override void ToString(IStringifyContext context, Expression[] x)
		{
			if(context.EnclosingOperator > StringifyOperator.Exponent)
				context.Emit("(");
			context.Emit(x[0], StringifyOperator.Exponent).Emit("^").Emit(x[1], StringifyOperator.Exponent);
			if(context.EnclosingOperator > StringifyOperator.Exponent)
				context.Emit(")");
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit("pow");
		}
	}
}
