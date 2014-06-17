using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Mappings;

namespace Reynolds.Expressions.Expressions
{
	public class SumExpression : Expression
	{
		public readonly Expression[] Terms;

		static WeakLazyMapping<Expression[], SumExpression> sumExpressions = new WeakLazyMapping<Expression[], SumExpression>(es => new SumExpression(es), null, ReferenceTypeArrayEqualityComparer<Expression>.Instance);

		static internal Expression Get(params Expression[] terms)
		{
			Dictionary<Expression, dynamic> newTerms = new Dictionary<Expression, dynamic>();
			dynamic constant = 0;

			Action<Expression> addTerm = delegate(Expression e)
			{
				if(e.IsConstant)
					constant += e.Value;
				else
				{
					ProductExpression pe = e as ProductExpression;
					dynamic coeff = 1;
					if(null != pe && pe.Factors[0].IsConstant && pe.Factors[0].IsScalar)
					{
						coeff = pe.Factors[0].Value;
						e = ProductExpression.Get(pe.Factors.Skip(1).ToArray());
					}

					dynamic val;
					if(!newTerms.TryGetValue(e, out val))
						val = 0;
					val += coeff;
					if(val == 0)
						newTerms.Remove(e);
					else
						newTerms[e] = val;
				}
			};

			foreach(var t in terms)
			{
				var sf = t;
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
				ts.Add(kv.Value * kv.Key);

			terms = ts.ToArray();

			if(terms.Length == 0)
				return 0;

			if(terms.Length == 1)
				return terms[0];

			Array.Sort<Expression>(terms);
			return sumExpressions[terms];
		}

		bool isScalar;
		public override bool IsScalar
		{
			get
			{
				return isScalar;
			}
		}

		protected SumExpression(Expression[] terms)
		{
			this.Terms = terms;
			isScalar = terms.All(e => e.IsScalar);
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

		public override void ToString(IStringifyContext context)
		{
			if(context.EnclosingOperator > StringifyOperator.Sum)
				context.Emit("(");
			bool first = true;
			for(int k = 0; k < Terms.Length; k++)
			{
				ProductExpression pe = Terms[k] as ProductExpression;
				if(!(pe != null && pe.Factors[0].IsNegative) || Terms[k].IsNegative)
					if(!first)
						context.Emit("+");
				context.Emit(Terms[k], StringifyOperator.Sum);
				first = false;
			}
			if(context.EnclosingOperator > StringifyOperator.Sum)
				context.Emit(")");
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit("(");
			bool first = true;
			for(int k = 0; k < Terms.Length; k++)
			{
				//CoefficientExpression ce = Terms[k] as CoefficientExpression;
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
