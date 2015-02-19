using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions.Functions
{
	//internal class BlackScholesFunction : FunctionExpression
	//{
	//   public override int Arity
	//   {
	//      get
	//      {
	//         return 2;
	//      }
	//   }

	//   public override object Evaluate(params object[] x)
	//   {
	//      double ttm = (double) x[0];
	//      double fwd = (double) x[1];
	//      double strike = (double) x[1];
	//      double vol = (double) x[2];

	//      double sqrt_v2ttm = vol * Math.Sqrt(ttm);
	//      double dp = Math.Log(fwd / strike) / sqrt_v2ttm + 0.5 * sqrt_v2ttm;
	//      double dm = dp - sqrt_v2ttm;

	//      return N(dp) * fwd - N(dm) * strike;
	//   }

	//   public override void GenerateCode(ICodeGenerationContext context)
	//   {
	//      context.Emit("YearFraction");
	//   }

	//   public override void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
	//   {
	//      context.Emit("((").Emit(arguments[1]).Emit("-").Emit(arguments[0]).Emit(").TotalDays/365.0)");
	//   }

	//   public override void ToString(IStringifyContext context)
	//   {
	//      context.Emit("YearFraction");
	//   }
	//}

	//public static class FinanceFunctions
	//{
	//   public static readonly FunctionExpression BlackScholes = new BlackScholesFunction();
	//}
}
