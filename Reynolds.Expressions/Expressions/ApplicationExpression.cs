using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Mappings;

namespace Reynolds.Expressions.Expressions
{
	public class TupleExpression : Expression
	{
		static WeakLazyMapping<Expression[], TupleExpression> instances = new WeakLazyMapping<Expression[], TupleExpression>(
			es => new TupleExpression(es),
			null,
			ReferenceTypeArrayEqualityComparer<Expression>.Instance);

		public readonly Expression[] Elements;

		public static Expression Get(params Expression[] elements)
		{
			if(elements.Length == 1)
				return elements[0];
			else
				return instances[elements];
		}

		TupleExpression(Expression[] elements)
		{
			this.Elements = elements;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			throw new NotImplementedException();
		}

		protected override Expression Substitute(VisitCache cache)
		{
			Expression[] dx = Elements;
			for(int k = 0; k < Elements.Length; k++)
				if(Elements[k] != cache[Elements[k]])
					dx = new Expression[Elements.Length];
			if(dx != Elements)
				for(int k = 0; k < Elements.Length; k++)
					dx[k] = cache[Elements[k]];
			return Get(dx);
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit("(");
			bool first = true;
			foreach(var e in this)
			{
				if(!first)
					context.Emit(", ");
				context.Emit(e);
				first = false;
			}
			context.Emit(")");
		}

		public override int Count
		{
			get
			{
				return this.Elements.Length;
			}
		}

		public override IEnumerator<Expression> GetEnumerator()
		{
			return ((IEnumerable<Expression>) this.Elements).GetEnumerator();
		}
	}

	public class ApplicationExpression : Expression
	{
		public readonly Expression Target;
		public readonly Expression Argument;

		static WeakLazyMapping<Expression, Expression, ApplicationExpression> instances = new WeakLazyMapping<Expression, Expression, ApplicationExpression>((obj, indices) => new ApplicationExpression(obj, indices));

		protected ApplicationExpression(Expression target, Expression argument)
		{
			this.Target = target;
			this.Argument = argument;

			this.Domain = Domain.Apply(target, argument);
		}

		protected override Expression Substitute(VisitCache cache)
		{
			var df = cache[Target];
			var dx = cache[Argument];

			return instances[df, dx];
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			throw new NotImplementedException();
			//List<Expression> terms = new List<Expression>();
			//for(int k = 0; k < Argument.Length; k++)
			//{
			//   var dx = cache[Argument[k]];
			//   if(!dx.IsZero)
			//      terms.Add(dx * Applicand.GetPartialDerivative(k, Argument));
			//}
			//if(terms.Count == 0)
			//   return 0;
			//else if(terms.Count == 1)
			//   return terms[0];
			//else
			//{
			//   terms.Sort();
			//   return SumExpression.Get(terms.ToArray());
			//}
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit(Target, new Expression[] { Argument });
		}

		protected static bool ComesBefore(Expression a, Expression b)
		{
			if(a.IsConstant && a.Domain <= Domain.Reals)
			{
				if(b.IsConstant && b.Domain <= Domain.Reals)
					return a.Value.CompareTo(b.Value) < 0;
				else
					return true;
			}
			else
			{
				if(b.IsConstant && b.Domain <= Domain.Reals)
					return false;
				else
					return a.CompareTo(b) < 0;
			}
		}

		public static Expression Get(Expression target, Expression argument)
		{
			ApplicationExpression ae;
			if(null != (ae = argument as ApplicationExpression) && Domain.AreAssociative(target.Domain, ae.Target.Domain, ae.Argument.Domain))
				return target.Apply(ae.Target).Apply(ae.Argument);

			if(null != (ae = target as ApplicationExpression))
			{
				if(Domain.AreCommutative(argument.Domain, ae.Target.Domain))
				{
					if(argument == ae.Target || ComesBefore(argument, ae.Target))
						return argument.Apply(target);
				}
			}
			else if(Domain.AreCommutative(target.Domain, argument.Domain) && ComesBefore(argument, target))
				return argument.Apply(target);

			return instances[target, argument];
		}
	}
}
