using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Mappings;

namespace Reynolds.Expressions
{
	public interface IDerivativeCache
	{
		Expression this[Expression expression, Expression variable]
		{
			get;
		}
	}

	public class DerivativeCache : IDerivativeCache
	{
		DictionaryMapping<Expression, Expression, Expression> cache = new DictionaryMapping<Expression, Expression, Expression>();

		public Expression this[Expression expression, Expression variable]
		{
			get
			{
				Expression derivative;
				if(!cache.TryGetValue(expression, variable, out derivative))
				{
					if(expression == variable)
						derivative = 1;
					else
						derivative = expression.Derive(this, variable);
					cache[expression, variable] = derivative;
				}
				return derivative;
			}
		}
	}

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
