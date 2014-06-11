using System;
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
			return (s == this) ? 1 : 0;
		}

		protected override Expression Normalize(INormalizeContext context)
		{
			return this;
		}

		public static ExpressionSubstitution operator |(Symbol symbol, Expression expression)
		{
			return new ExpressionSubstitution(symbol, expression);
		}

		public override string ToString()
		{
			return this.Name;
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit(this.Name);
		}
	}
}
