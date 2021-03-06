﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Mappings;

namespace Reynolds.Expressions.Expressions
{
	internal class ApplicationExpression : Expression
	{
		public Expression Applicand;
		public readonly Expression[] Arguments;

		//bool isScalar;
		//public override bool IsScalar
		//{
		//   get
		//   {
		//      return isScalar;
		//   }
		//}

		static WeakLazyMapping<Expression, Expression[], ApplicationExpression> instances = new WeakLazyMapping<Expression, Expression[], ApplicationExpression>(
			(obj, indices) => new ApplicationExpression(obj, indices),
			null,
			null,
			ReferenceTypeArrayEqualityComparer<Expression>.Instance
			);

		protected ApplicationExpression(Expression f, Expression[] x)
		{
			this.Applicand = f;
			this.Arguments = x;
			//isScalar = f.GetIsScalar(x);
		}

		protected override Expression Substitute(VisitCache cache)
		{
			var df = cache[Applicand];
			Expression[] dx = Arguments;
			for(int k = 0; k < Arguments.Length; k++)
				if(Arguments[k] != cache[Arguments[k]])
					dx = new Expression[Arguments.Length];
			if(dx != Arguments)
				for(int k = 0; k < Arguments.Length; k++)
					dx[k] = cache[Arguments[k]];
			if(Applicand != df || Arguments != dx)
				return df[dx];
			else
				return this;
		}

		internal override Expression Derive(IDerivativeCache cache, Expression variable)
		{
			List<Expression> terms = new List<Expression>();
			for(int k = 0; k < Arguments.Length; k++)
			{
				var dx = cache[Arguments[k], variable];
				if(!dx.IsZero)
					terms.Add(dx * Applicand.GetPartialDerivative(k, Arguments));
			}
			if(terms.Count == 0)
				return 0;
			else if(terms.Count == 1)
				return terms[0];
			else
			{
				terms.Sort();
				return SumExpression.Get(terms.ToArray());
			}
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit(Applicand, Arguments);
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit(Applicand, Arguments);
		}

		public static Expression Get(Expression applicand, Expression[] arguments)
		{
			var alt = applicand.Normalize(arguments);
			if(alt == null)
				return instances[applicand, arguments];
			else
				return alt;
		}
	}
}
