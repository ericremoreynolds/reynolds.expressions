﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Mappings;

namespace Reynolds.Expressions.Expressions
{
	internal class SumExpression : Expression
	{
		public readonly Expression[] Terms;

		static Dictionary<Expression[], SumExpression> sumExpressions = new Dictionary<Expression[], SumExpression>(ReferenceTypeArrayEqualityComparer<Expression>.Instance);
		static internal Expression Get(params Expression[] terms)
		{
			if(terms.Length == 0)
				return 0;

			if(terms.Length == 1)
				return terms[0];

			Array.Sort<Expression>(terms);
			SumExpression e;
			if(!sumExpressions.TryGetValue(terms, out e))
				sumExpressions[terms] = e = new SumExpression(terms);
			return e;
		}

		protected SumExpression(Expression[] terms)
		{
			this.Terms = terms;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			bool changed = false;
			Expression[] newTerms = new Expression[Terms.Length];
			for(int k = 0; k < Terms.Length; k++)
			{
				newTerms[k] = cache[Terms[k]];
				changed = changed || newTerms[k] != Terms[k];
			}
			if(changed)
				return Get(newTerms);
			return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			List<Expression> terms = new List<Expression>();
			foreach(var t in Terms)
			{
				var dt = cache[t];
				if(!dt.IsZero)
					terms.Add(dt);
			}
			return SumExpression.Get(terms.ToArray());
		}

		protected override Expression Normalize(INormalizeContext context)
		{
			Dictionary<Expression, dynamic> newTerms = new Dictionary<Expression, dynamic>();
			dynamic constant = 0;

			Action<Expression> addTerm = delegate(Expression e)
			{
				if(e.IsConstant)
					constant += e.Value;
				else
				{
					CoefficientExpression ce = e as CoefficientExpression;
					if(null != ce)
					{
						dynamic val;
						if(!newTerms.TryGetValue(ce.Expression, out val))
							val = 0;
						val += ce.Coefficient;
						if(val == 0)
							newTerms.Remove(ce.Expression);
						else
							newTerms[ce.Expression] = val;
					}
					else
					{
						dynamic val;
						if(!newTerms.TryGetValue(e, out val))
							val = 0;
						val += 1;
						if(val == 0)
							newTerms.Remove(e);
						else
							newTerms[e] = val;
					}
				}
			};

			foreach(var t in Terms)
			{
				var sf = context.Normalize(t);
				var sumEx = sf as SumExpression;
				if(sumEx != null)
				{
					foreach(var st in sumEx.Terms)
						addTerm(st);
				}
				else
					addTerm(sf);
			}

			List<Expression> ts = new List<Expression>();
			if(constant != 0)
				ts.Add(constant);
			foreach(var kv in newTerms)
				if(kv.Value != 1)
					ts.Add(CoefficientExpression.Get(kv.Value, kv.Key));
				else
					ts.Add(kv.Key);
			return Get(ts.ToArray());
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("(");
			bool first = true;
			for(int k = 0; k < Terms.Length; k++)
			{
				CoefficientExpression ce = Terms[k] as CoefficientExpression;
				if( /*!(ce != null && ce.Coefficient < 0.0)&& */ !(Terms[k].IsConstant && Terms[k].Value < 0))
					if(!first)
						sb.Append("+");
				sb.Append(Terms[k].ToString());
				first = false;
			}
			sb.Append(")");
			return sb.ToString();
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit("(");
			bool first = true;
			for(int k = 0; k < Terms.Length; k++)
			{
				CoefficientExpression ce = Terms[k] as CoefficientExpression;
				if(/*!(ce != null && ce.Coefficient < 0.0) &&*/ !(Terms[k].IsConstant && Terms[k].Value < 0))
					if(!first)
						context.Emit("+");
				context.Emit(Terms[k]);
				first = false;
			}
			context.Emit(")");
		}
	}
}
