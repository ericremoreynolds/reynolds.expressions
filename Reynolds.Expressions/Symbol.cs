﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions
{
	public class Symbol : Expression
	{
		public Symbol(string name)
		{
			Name = name;
		}

		public string Name
		{
			get;
			protected set;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			return (s == this) ? Expression.One : Expression.Zero;
		}

		protected override Expression Simplify(VisitCache cache)
		{
			return this;
		}

		public static ExpressionSubstitution operator |(Symbol symbol, Expression expression)
		{
			return new ExpressionSubstitution(symbol, expression);
		}

		public static ExpressionSubstitution operator |(Symbol symbol, object anything)
		{
			return new ExpressionSubstitution(symbol, Expression.Constant(anything));
		}

		public override string ToString()
		{
			return this.Name;
		}

		public override string ToCode()
		{
			return this.Name;
		}
	}
}
