﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions
{
	public class SymbolExpression : Expression
	{
		public static SymbolExpression Get(string name, Domain domain)
		{
			return new SymbolExpression(name, domain);
		}

		protected SymbolExpression(string name, Domain domain)
		{
			Name = name;
			Domain = domain;
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
			return (s == this) ? 1 : 0;
		}

		public static ExpressionSubstitution operator |(SymbolExpression symbol, Expression expression)
		{
			return new ExpressionSubstitution(symbol, expression);
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit(this.Name);
		}

		public override void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
		{
			if(arguments.Length == 0)
				context.Emit(this.Name);
		}
	}
}
