using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions
{
	public class Symbol : Expression
	{
		bool isScalar;
		public override bool IsScalar
		{
			get
			{
				return isScalar;
			}
		}

		public Symbol(string name, bool scalar = true)
		{
			Name = name;
			isScalar = scalar;
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

		public static ExpressionSubstitution operator |(Symbol symbol, Expression expression)
		{
			return new ExpressionSubstitution(symbol, expression);
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit(this.Name);
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit(this.Name);
		}

		public override bool GetIsScalar(Expression[] arguments)
		{
			return isScalar;
		}
	}
}
