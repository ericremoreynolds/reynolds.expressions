﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Expressions.Expressions;
using Reynolds.Mappings;

namespace Reynolds.Expressions
{
	public abstract class FunctionExpression : Expression
	{
		public abstract int Arity
		{
			get;
		}

		public override bool IsConstant
		{
			get
			{
				return true;
			}
		}

		public abstract double Evaluate(params double[] x);

		public override Expression this[params Expression[] arguments]
		{
			get
			{
				if(arguments.Length != Arity)
					throw new Exception("Wrong number of arguments.");

				return base[arguments];
			}
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			throw new NotImplementedException();
		}

		public override void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
		{
			context.Emit(this).Emit("(");
			for(int k = 0; k < arguments.Length; k++)
			{
				if(k > 0)
					context.Emit(", ");
				context.Emit(arguments[k]);
			}
			context.Emit(")");
		}

		public override void ToString(IStringifyContext context, Expression[] arguments)
		{
			context.Emit(this, StringifyOperator.Application).Emit("[");
			for(int k = 0; k < arguments.Length; k++)
			{
				if(k > 0)
					context.Emit(", ");
				context.Emit(arguments[k]);
			}
			context.Emit("]");
		}

		public override Expression Apply(Expression argument)
		{
			if(argument.All(a => a.IsConstant))
				return Evaluate((from x in argument select Convert.ToDouble((object) x.Value)).ToArray());
			else
				return base.Apply(argument);
		}

		protected abstract Expression GetPartialDerivative(int i, Expression[] x);
	}
}
