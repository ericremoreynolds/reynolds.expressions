using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Mappings;

namespace Reynolds.Expressions.Expressions
{
	internal class ProductExpression : Expression
	{
		public readonly Expression[] Factors;

		static Dictionary<Expression[], ProductExpression> productExpressions = new Dictionary<Expression[], ProductExpression>(ReferenceTypeArrayEqualityComparer<Expression>.Instance);
		static internal Expression Get(params Expression[] terms)
		{
			if(terms.Length == 0)
				return 1;

			if(terms.Length == 1)
				return terms[0];

			Array.Sort<Expression>(terms);
			ProductExpression e;
			if(!productExpressions.TryGetValue(terms, out e))
				productExpressions[terms] = e = new ProductExpression(terms);
			return e;
		}

		protected ProductExpression(Expression[] factors)
		{
			this.Factors = factors;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			bool changed = false;
			Expression[] newFactors = new Expression[Factors.Length];
			for(int k = 0; k < Factors.Length; k++)
			{
				newFactors[k] = cache[Factors[k]];
				changed = changed || newFactors[k] != Factors[k];
			}
			if(changed)
				return Get(newFactors);
			return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			List<Expression> terms = new List<Expression>();
			foreach(var f in Factors)
			{
				var df = cache[f];
				if(!df.IsZero)
					terms.Add(df * this / f);
			}
			return SumExpression.Get(terms.ToArray());
		}

		protected override Expression Normalize(INormalizeContext context)
		{
			Dictionary<Expression, Expression> newFactors = new Dictionary<Expression, Expression>();
			dynamic coefficient = 1;

			Action<Expression> addFactor = delegate(Expression e)
			{
				if(e.IsConstant)
					coefficient *= e.Value;
				else
				{
					ApplicationExpression ae = e as ApplicationExpression;
					if(null != ae && ae.Applicand == Expression.Pow)
					{
						Expression p;
						if(!newFactors.TryGetValue(ae.Arguments[0], out p))
							p = 0;
						p += ae.Arguments[1];
						newFactors[ae.Arguments[0]] = p;
					}
					else
					{
						Expression p;
						if(!newFactors.TryGetValue(e, out p))
							p = 0;
						p += 1;
						newFactors[e] = p;
					}
				}
			};

			foreach(var t in Factors)
			{
				var sf = context.Normalize(t);
				var ce = sf as CoefficientExpression;
				if(ce != null)
				{
					coefficient *= ce.Coefficient;
					sf = ce.Expression;
				}
				var pe = sf as ProductExpression;
				if(pe != null)
				{
					foreach(var pef in pe.Factors)
						addFactor(pef);
				}
				else
					addFactor(sf);
			}

			List<Expression> fs = new List<Expression>();
			foreach(var kv in newFactors)
			{
				var p = context.Normalize(kv.Value);
				if(p.IsConstant)
				{
					if(kv.Key.IsConstant)
						coefficient *= Math.Pow(kv.Key.Value, p.Value);
					else if(p.Value == 1)
						fs.Add(kv.Key);
					else if(p.Value != 0)
						fs.Add(Expression.Pow[kv.Key, p]);
				}
				else
					fs.Add(Expression.Pow[kv.Key, p]);
			}
			if(coefficient == 1)
				return Get(fs.ToArray());
			else
				return context.Normalize(CoefficientExpression.Get(coefficient, Get(fs.ToArray())));
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("(");
			bool first = true;
			for(int k = 0; k < Factors.Length; k++)
			{
				if(!first)
					sb.Append(" ");
				sb.Append(Factors[k].ToString());
				first = false;
			}
			sb.Append(")");
			return sb.ToString();
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit("(");
			bool first = true;
			for(int k = 0; k < Factors.Length; k++)
			{
				if(!first)
					context.Emit("*");
				context.Emit(Factors[k]);
				first = false;
			}
			context.Emit(")");
		}
	}
}
