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

		protected override Expression Normalize(INormalizeContext context)
		{
			return this;
		}

		public override string ToCode(Expression[] arguments)
		{
			return this.ToCode() + "(" + string.Join(", ", arguments.Select(a => a.ToCode()).ToArray()) + ")";
		}

		public override string ToString(Expression[] arguments)
		{
			return this.ToString() + "[" + string.Join(", ", arguments.Select(a => a.ToString()).ToArray()) + "]";
		}

		protected override Expression Normalize(INormalizeContext context, Expression[] arguments)
		{
			if(arguments.All(a => a.IsConstant))
				return Evaluate((from x in arguments select Convert.ToDouble((object) x.Value)).ToArray());
			else
				return this[arguments];
		}
	}
}
