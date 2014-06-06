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

		public virtual Expression TrySimplify(params Expression[] x)
		{
			return null; //	this[x];
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			throw new NotImplementedException();
		}

		protected override Expression Normalize(VisitCache cache)
		{
			return this;
		}

		public override string ToCode()
		{
			throw new NotImplementedException();
		}

		public override Expression Normalize(Expression[] arguments)
		{
			if(arguments.All(a => a.IsConstant))
				return Evaluate((from x in arguments select Convert.ToDouble((object) x.Value)).ToArray());
			else
			{
				var df = TrySimplify(arguments);
				if(df != null)
					return df.Normalize(); //cache[df]; // TODO: not using cache here
				else
					return this[arguments];
			}
		}
	}
}
