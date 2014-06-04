using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions
{
	public class VisitCache
	{
		public delegate Expression Visitor(Expression f, VisitCache c);

		Visitor visitor;

		public VisitCache(Visitor visitor)
		{
			this.visitor = visitor;
		}

		Dictionary<Expression, Expression> cache = new Dictionary<Expression, Expression>();

		public Expression this[Expression f]
		{
			get
			{
				Expression g;
				if(!cache.TryGetValue(f, out g))
					cache[f] = g = visitor(f, this);
				return g;
			}
		}

		public void Add(Expression f, Expression g)
		{
			cache.Add(f, g);
		}
	}
}
