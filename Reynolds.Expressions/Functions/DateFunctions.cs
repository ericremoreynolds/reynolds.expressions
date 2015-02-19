using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions.Functions
{
	internal class YearFraction : FunctionExpression
	{
		public override int Arity
		{
			get
			{
				return 2;
			}
		}

		public override object Evaluate(params object[] x)
		{
			DateTime baseDate = (DateTime) x[0];
			DateTime date = (DateTime) x[1];
			return (date - baseDate).TotalDays / 365.0;
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit("YearFraction");
		}

		public override void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
		{
			context.Emit("((").Emit(arguments[1]).Emit("-").Emit(arguments[0]).Emit(").TotalDays/365.0)");
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit("YearFraction");
		}
	}

	internal class YearFractionAct36525Hours : FunctionExpression
	{
		public override int Arity
		{
			get
			{
				return 2;
			}
		}

		public override object Evaluate(params object[] x)
		{
			DateTime baseDate = (DateTime) x[0];
			DateTime date = (DateTime) x[1];
			return (date - baseDate).TotalHours / 24.0 / 365.25;
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit("YearFractionActActHours");
		}

		public override void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
		{
			context.Emit("((").Emit(arguments[1]).Emit("-").Emit(arguments[0]).Emit(").YearFractionActActHours)");
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit("YearFractionActActHours");
		}
	}

	public static class DateFunctions
	{
		public static readonly FunctionExpression YearFraction = new YearFraction();
		public static readonly FunctionExpression YearFractionAct36525Hours = new YearFractionAct36525Hours();
	}
}
