using System;
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

		//public override Expression this[params Expression[] arguments]
		//{
		//   get
		//   {

		//   }
		//}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		public override Expression Derive(Expression[] arguments, Expression s)
		{
			if(arguments.Length == 1)
			{
				var x = arguments[0].ToArray();

				List<Expression> terms = new List<Expression>();
				for(int k = 0; k < x.Length; k++)
				{
					var dx = x[k].Derive(s);
					if(!dx.IsZero)
						terms.Add(dx * this.GetPartialDerivative(k, x));
				}
				return SumExpression.Get(terms.ToArray());
			}

			return base.Derive(arguments, s);
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

		public override Expression Apply(params Expression[] arguments)
		{
			//if(arguments.Length != Arity)
			//   throw new Exception("Wrong number of arguments.");

			//return base[arguments];

			if(arguments.Length == 1 && arguments[0].All(a => a.IsConstant))
			{
				if(arguments[0].Count != Arity)
					throw new Exception("Wrong number of arguments");
				
				return Evaluate((from x in arguments[0] select Convert.ToDouble((object) x.Value)).ToArray());
			}
			else
				return base.Apply(arguments);
		}

		protected abstract Expression GetPartialDerivative(int i, Expression[] x);
	}
}
