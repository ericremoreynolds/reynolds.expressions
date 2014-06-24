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

		public override Expression GetElement(int k)
		{
			return this.Elements[k];
		}

		public override IEnumerator<Expression> GetEnumerator()
		{
			return ((IEnumerable<Expression>) this.Elements).GetEnumerator();
		}
	}
}
