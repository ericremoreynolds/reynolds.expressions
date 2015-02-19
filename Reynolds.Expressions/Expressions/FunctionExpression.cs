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

		public abstract object Evaluate(params object[] x);

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

		internal override Expression Derive(IDerivativeCache cache, Expression s)
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

		public override Expression Normalize(Expression[] arguments)
		{
			if(arguments.All(a => a.IsConstant))
				//return Evaluate((from x in arguments select Convert.ToDouble((object) x.Value)).ToArray());
				return Expression.Constant(Evaluate((from x in arguments select (object) x.Value).ToArray()));
			else
				return base.Normalize(arguments);
		}

		//public override bool GetIsScalar(Expression[] arguments)
		//{
		//   return arguments.All(e => e.IsScalar);
		//}
	}
}
