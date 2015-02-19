using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Mappings;

namespace Reynolds.Expressions.Expressions
{
	public class CollectionSumExpression : Expression
	{
		public readonly Expression Collection;
		public readonly Symbol Index;
		public readonly Expression Term;

		protected static readonly Symbol placeholder = new Symbol("#");

		static DictionaryMapping<Expression, Expression, CollectionSumExpression> sumExpressions = new DictionaryMapping<Expression, Expression, CollectionSumExpression>();

		static internal Expression Get(Expression collection, Func<Symbol, Expression> term)
		{
			Expression weakExpr = term(placeholder);
			CollectionSumExpression ret;
			if(!sumExpressions.TryGetValue(collection, weakExpr, out ret))
			{
				var index = new Symbol("n");
				sumExpressions[collection, weakExpr] = ret = new CollectionSumExpression(collection, index, term(index));
			}
			return ret;
		}

		protected CollectionSumExpression(Expression collection, Symbol index, Expression term)
		{
			this.Collection = collection;
			this.Index = index;
			this.Term = term;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			Expression newCollection = cache[Collection];
			Expression newTerm = cache[Term];
			if(newCollection != Collection || newTerm != Term)
				return Get(newCollection, n => newTerm.Substitute(Index | n));
			else
				return this;
		}

		internal override Expression Derive(IDerivativeCache cache, Expression variable)
		{
			Expression dterm = cache[Term, variable];
			if(dterm.IsZero)
				return 0;
			else
				return Get(Collection, n => dterm.Substitute(Index | n));
		}

		public override void ToString(IStringifyContext context)
		{
			if(context.EnclosingOperator > StringifyOperator.Sum)
				context.Emit("(");
			context.Emit("sum ");
			context.Emit(Index);
			context.Emit(" in ");
			context.Emit(Collection);
			context.Emit(" : ");
			context.Emit(Term);
			if(context.EnclosingOperator > StringifyOperator.Sum)
				context.Emit(")");
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit(Collection);
			context.Emit(".Sum(");
			context.Emit(Index);
			context.Emit(" => ");
			context.Emit(Term);
			context.Emit(")");
		}
	}

	public class DynamicSumExpression : Expression
	{
		public readonly Expression From;
		public readonly Expression To;
		public readonly Symbol Index;
		public readonly Expression Term;

		protected static readonly Symbol placeholder = new Symbol("#");

		static DictionaryMapping<Expression, Expression, Expression, DynamicSumExpression> sumExpressions = new DictionaryMapping<Expression, Expression, Expression, DynamicSumExpression>();

		static internal Expression Get(Expression from, Expression to, Func<Symbol, Expression> term)
		{
			Expression weakExpr = term(placeholder);
			DynamicSumExpression ret;
			if(!sumExpressions.TryGetValue(from, to, weakExpr, out ret))
			{
				var index = new Symbol("n");
				sumExpressions[from, to, weakExpr] = ret = new DynamicSumExpression(from, to, index, term(index));
			}
			return ret;
		}

		protected DynamicSumExpression(Expression from, Expression to, Symbol index, Expression term)
		{
			this.From = from;
			this.To = to;
			this.Index = index;
			this.Term = term;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			Expression newFrom = cache[From];
			Expression newTo = cache[To];
			Expression newTerm = cache[Term];
			if(newFrom != From || newTo != To || newTerm != Term)
				return Get(newFrom, newTo, n => newTerm.Substitute(Index | n));
			else
				return this;
		}

		internal override Expression Derive(IDerivativeCache cache, Expression variable)
		{
			Expression dterm = cache[Term, variable];
			if(dterm.IsZero)
				return 0;
			else
				return Get(From, To, n => dterm.Substitute(Index | n));
		}

		public override void ToString(IStringifyContext context)
		{
			if(context.EnclosingOperator > StringifyOperator.Sum)
				context.Emit("(");
			context.Emit("sum ");
			context.Emit(Index);
			context.Emit(" in ");
			context.Emit(From);
			context.Emit(" to ");
			context.Emit(To);
			context.Emit(" : ");
			context.Emit(Term);
			if(context.EnclosingOperator > StringifyOperator.Sum)
				context.Emit(")");
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit("Enumerable.Range(");
			context.Emit(From);
			context.Emit(", ");
			context.Emit(To);
			context.Emit(").Sum(");
			context.Emit(Index);
			context.Emit(" => ");
			context.Emit(Term);
			context.Emit(")");
		}
	}

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
					if(null != pe && pe.Factors[0].IsConstant) // && pe.Factors[0].IsScalar)
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

		//bool isScalar;
		//public override bool IsScalar
		//{
		//   get
		//   {
		//      return isScalar;
		//   }
		//}

		protected SumExpression(Expression[] terms)
		{
			this.Terms = terms;
			//isScalar = terms.All(e => e.IsScalar);
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

		internal override Expression Derive(IDerivativeCache cache, Expression variable)
		{
			List<Expression> terms = new List<Expression>();
			foreach(var t in Terms)
			{
				var dt = cache[t, variable];
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
