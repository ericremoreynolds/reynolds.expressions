﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions.Expressions
{
	public class FieldExpression : Expression
	{
		public readonly string FieldName;

		static Dictionary<string, FieldExpression> cache = new Dictionary<string, FieldExpression>();
		public static Expression Get(string name)
		{
			FieldExpression e;
			if(!cache.TryGetValue(name, out e))
				cache[name] = e = new FieldExpression(name);
			return e;
		}

		FieldExpression(string name)
		{
			this.FieldName = name;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		internal override Expression Derive(IDerivativeCache cache, Expression s)
		{
			return 0;
		}

		public override bool IsConstant
		{
			get
			{
				return true;
			}
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit(FieldName);
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit(FieldName);
		}
	}
}
