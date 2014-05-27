using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;
using Reynolds.Mappings;

namespace Reynolds.Expressions
{
	public class VisitCache
	{
		public delegate Expression Visitor(Expression f, VisitCache c);

		Visitor visitor;

		public VisitCache(Visitor visitor)
		{
			this.visitor = visitor;
		}

		Dictionary<Expression, Expression> cache = new Dictionary<Expression,Expression>();

		public Expression this[Expression f]
		{
			get
			{
				Expression g;
				if(!cache.TryGetValue(f, out g))
					cache[f] = g = visitor(f, this);
				return g;
			}
		}

		public void Add(Expression f, Expression g)
		{
			cache.Add(f, g);
		}
	}

	public delegate double CompiledExpression(params double[] x);

	public abstract class Expression : IComparable<Expression>
	{
		private static int ordinalCounter = Int32.MinValue;
		private int ordinal;

		protected Expression()
		{
			if(ordinalCounter == Int32.MaxValue)
				throw new Exception("Exhausted ordinals.");

			ordinal = ordinalCounter++;
		}

		public Expression Substitute(params ExpressionSubstitution[] substitutions)
		{
			VisitCache cache = new VisitCache((f, c) => f.Substitute(c));
			int count = 0;
			foreach(var substitution in substitutions)
			{
				if(substitution.Expression != substitution.Substitute)
				{
					count++;
					cache.Add(substitution.Expression, substitution.Substitute);
				}
			}
			if(count > 0)
				return cache[this];
			else
				return this;
		}

		public Expression Derive(Expression s)
		{
			VisitCache cache = new VisitCache((f, c) => f.Derive(c, s));
			return cache[this];
		}

		public Expression Simplify()
		{
			VisitCache cache = new VisitCache((f, c) => f.Simplify(c));
			return cache[this];
		}

		protected abstract Expression Substitute(VisitCache cache);
		protected abstract Expression Derive(VisitCache cache, Expression s);
		protected abstract Expression Simplify(VisitCache cache);

		public virtual bool IsConstant
		{
			get
			{
				return false;
			}
		}

		public virtual bool IsZero
		{
			get
			{
				return false;
			}
		}

		public virtual bool IsOne
		{
			get
			{
				return false;
			}
		}

		public virtual double Value
		{
			get
			{
				throw new Exception("Function is not fully evaluated.");
			}
		}

		public static readonly Expression Zero = 0.0;
		public static readonly Expression One = 1.0;

		public static implicit operator Expression(double a)
		{
			return ConstantExpression.Get(a);
		}

		public static Expression operator +(Expression f, Expression g)
		{
			return SumExpression.Get(f, g);
		}

		public static Expression operator -(Expression f, Expression g)
		{
			return SumExpression.Get(f, CoefficientExpression.Get(-1.0, g));
		}

		public static Expression operator *(Expression f, Expression g)
		{
			return ProductExpression.Get(f, g);
		}

		public static Expression operator /(Expression f, Expression g)
		{
			return ProductExpression.Get(f, Expression.Pow[g, -1]);
		}

		public static Expression operator -(Expression f)
		{
			return CoefficientExpression.Get(-1.0, f);
		}

		public Expression this[params Expression[] indices]
		{
			get
			{
				return AccessExpression.Get(this, indices);
			}
		}

		public static readonly Function Log = new DelegateFunction(
			"log", "Math.Log",
			x => Math.Log(x[0]),
			x => 1.0 / x[0]
			);

		public static readonly Function Exp = new DelegateFunction(
			"exp", "Math.Exp",
			x => Math.Exp(x[0]),
			x => Exp[x[0]]
			);

		public static readonly Function Pow = new DelegateFunction(
			"pow", "Math.Pow",
			x => Math.Pow(x[0], x[1]),
			x => x[1] * Pow[x[0], x[1] - 1],
			x => Log[x[0]] * Pow[x[0], x[1]]
			);

		public static readonly Function Sin = new DelegateFunction(
			"sin", "Math.Sin",
			x => Math.Sin(x[0]),
			null // set after
			);

		public static readonly Function Cos = new DelegateFunction(
			"cos", "Math.Cos",
			x => Math.Cos(x[0]),
			null // set after
			);
		
		static Expression()
		{
			((DelegateFunction) Sin).SetPartial(0, x => -Cos[x[0]]);
			((DelegateFunction) Cos).SetPartial(0, x => Sin[x[0]]);
			((DelegateFunction) Pow).Stringify = (x) => (x[0].ToString() + "^" + x[1].ToString());

			((DelegateFunction) Pow).Simplify = delegate(Expression[] x)
			{
				var pe = x[0] as ProductExpression;
				CoefficientExpression ce;
				if(pe != null)
					return ProductExpression.Get((from f in pe.Factors select Expression.Pow[f, x[1]]).ToArray());
				else if(null != (ce = x[0] as CoefficientExpression))
					return ProductExpression.Get(Expression.Pow[ce.Coefficient, x[1]], Expression.Pow[ce.Expression, x[1]]);
				else
					return null;
			};

			((DelegateFunction) Pow).Codify = delegate(Expression[] x)
			{
				if(x[1].IsConstant && x[1].Value == -1.0)
					return "(1d/" + x[0].ToString() + ")";
				else
					return null;
			};
		}

		public static MethodInfo[] Compile(Expression[] es, Symbol[] x, Type[] types)
		{
			ExpressionSubstitution[] subs = new ExpressionSubstitution[x.Length];
			for(int k = 0; k < x.Length; k++)
				subs[k] = x[k] | new Symbol("x" + k.ToString());
			es = (from e in es
					select e.Substitute(subs)).ToArray();

			StringBuilder sb = new StringBuilder();
			sb.Append("using System;");
			sb.Append("public static class GeneratedFunction {");
			for(int k = 0; k < es.Length; k++)
			{
				sb.Append("public static double f" + k.ToString() + "(" + string.Join(", ", (from i in Enumerable.Range(0, x.Length) select types[i].FullName + " x" + i.ToString())) + ") { return " + es[k].ToCode() + "; }");
			}
			sb.Append("}");
			string code = sb.ToString();

			CompilerParameters parameters = new CompilerParameters();
			parameters.GenerateInMemory = true;
			parameters.TreatWarningsAsErrors = false;
			parameters.GenerateExecutable = false;
			parameters.CompilerOptions = "/optimize";
			parameters.IncludeDebugInformation = false;
			parameters.ReferencedAssemblies.Add("System.dll");
			foreach(var typ in types)
				parameters.ReferencedAssemblies.Add(typ.Assembly.Location);

			CompilerResults results = new CSharpCodeProvider().CompileAssemblyFromSource(parameters, new string[] { code });

			if(results.Errors.HasErrors)
				throw new Exception("Compile error: " + results.Errors[0].ToString()); //, results.Errors);

			MethodInfo[] ces = new MethodInfo[es.Length];
			for(int k = 0; k < es.Length; k++)
			{
				var mi = results.CompiledAssembly.GetModules()[0].GetType("GeneratedFunction").GetMethod("f" + k.ToString());
				ces[k] = mi;
			}

			return ces;
		}

		public static CompiledExpression[] Compile(Expression[] es, params Symbol[] x)
		{
			ExpressionSubstitution[] subs = new ExpressionSubstitution[x.Length];
			for(int k = 0; k < x.Length; k++)
				subs[k] = x[k] | new Symbol("x[" + k.ToString() + "]");
			es = (from e in es select e.Substitute(subs)).ToArray();

			StringBuilder sb = new StringBuilder();
			sb.Append("using System;");
			sb.Append("public static class GeneratedFunction {");
			for(int k=0; k<es.Length; k++)
			{
				sb.Append("public static double f" + k.ToString() + "(params double[] x) { return " + es[k].ToCode() + "; }");
			}
			sb.Append("}");
			string code = sb.ToString();

			CompilerParameters parameters = new CompilerParameters();
			parameters.GenerateInMemory = true;
			parameters.TreatWarningsAsErrors = false;
			parameters.GenerateExecutable = false;
			parameters.CompilerOptions = "/optimize";
			parameters.IncludeDebugInformation = false;
			parameters.ReferencedAssemblies.Add("System.dll");

			CompilerResults results = new CSharpCodeProvider().CompileAssemblyFromSource(parameters, new string[] { code });

			if (results.Errors.HasErrors)
				throw new Exception("Compile error: " + results.Errors[0].ToString()); //, results.Errors);

			CompiledExpression[] ces = new CompiledExpression[es.Length];
			for(int k = 0; k < es.Length; k++)
			{
				var mi = results.CompiledAssembly.GetModules()[0].GetType("GeneratedFunction").GetMethod("f" + k.ToString());
				ces[k] = Delegate.CreateDelegate(typeof(CompiledExpression), mi) as CompiledExpression;
			}

			return ces;
		}

		public static CompiledExpression Compile(Expression e, params Symbol[] x)
		{
			return Compile(new Expression[] { e }, x)[0];
		}

		public CompiledExpression Compile(params Symbol[] x)
		{
			return Compile(this, x);
		}

		public Func<T1, double> Compile<T1>(Symbol x1)
		{
			return (Func<T1, double>) Delegate.CreateDelegate(typeof(Func<T1, double>), Compile(
				new Expression[] {this},
				new Symbol[] {x1},
				new Type[] { typeof(T1) })[0]);
		}

		public Func<T1, T2, T3, T4, T5, double> Compile<T1, T2, T3, T4, T5>(Symbol x1, Symbol x2, Symbol x3, Symbol x4, Symbol x5)
		{
			return (Func<T1, T2, T3, T4, T5, double>) Delegate.CreateDelegate(typeof(Func<T1, T2, T3, T4, T5, double>), Compile(
				new Expression[] { this },
				new Symbol[] { x1, x2, x3, x4, x5 },
				new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) })[0]);
		}

		public abstract string ToCode();

		public int CompareTo(Expression other)
		{
			return this.ordinal.CompareTo(other.ordinal);
		}

		public static Expression Constant(object obj)
		{
			if(obj is double)
				return (double) obj;
			else
				return ObjectConstantExpression.Get(obj);
		}

		public static Expression Field(string name)
		{
			return FieldExpression.Get(name);
		}

		public Expression _(string name)
		{
			return this[FieldExpression.Get(name)];
		}
	}

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

		public static ExpressionSubstitution operator| (Symbol symbol, Expression expression)
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

	internal class ConstantExpression : Expression
	{
		static WeakLazyMapping<double, ConstantExpression> constants = new WeakLazyMapping<double, ConstantExpression>(value => new ConstantExpression(value));
		public static Expression Get(double value)
		{
			return constants[value];
		}

		double value;

		public ConstantExpression(double value)
		{
			this.value = value;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			return Expression.Zero;
		}

		protected override Expression Simplify(VisitCache cache)
		{
			return this;
		}

		public override bool IsConstant
		{
			get
			{
				return true;
			}
		}

		public override bool IsZero
		{
			get
			{
				return value == 0.0;
			}
		}

		public override bool IsOne
		{
			get
			{
				return value == 1.0;
			}
		}

		public override double Value
		{
			get
			{
				return value;
			}
		}

		public override string ToString()
		{
			return value.ToString();
		}

		public override string ToCode()
		{
			return value.ToString() + "d";
		}
	}

	internal class FieldExpression : Expression
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

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			return Expression.Zero;
		}

		protected override Expression Simplify(VisitCache cache)
		{
			return this;
		}

		public override bool IsConstant
		{
			get
			{
				return true;
			}
		}

		public override string ToString()
		{
			return FieldName;
		}

		public override string ToCode()
		{
			return FieldName;
		}
	}

	internal class ObjectConstantExpression : Expression
	{
		public readonly object Object;

		static Dictionary<object, ObjectConstantExpression> cache = new Dictionary<object, ObjectConstantExpression>();
		public static Expression Get(object obj)
		{
			ObjectConstantExpression e;
			if(!cache.TryGetValue(obj, out e))
				cache[obj] = e = new ObjectConstantExpression(obj);
			return e;
		}

		ObjectConstantExpression(object obj)
		{
			this.Object = obj;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			return Expression.Zero;
		}

		protected override Expression Simplify(VisitCache cache)
		{
			return this;
		}

		public override bool IsConstant
		{
			get
			{
				return true;
			}
		}

		public override string ToString()
		{
			return Object.ToString();
		}

		public override string ToCode()
		{
			return Object.ToString() + "d";
		}
	}

	public class ExpressionSubstitution
	{
		public ExpressionSubstitution(Expression expression, Expression substitute)
		{
			this.Expression = expression;
			this.Substitute = substitute;
		}

		public Expression Expression
		{
			get;
			protected set;
		}

		public Expression Substitute
		{
			get;
			protected set;
		}
	}

	internal class SumExpression : Expression
	{
		public readonly Expression[] Terms;

		static Dictionary<Expression[], SumExpression> sumExpressions = new Dictionary<Expression[], SumExpression>(ReferenceTypeArrayEqualityComparer<Expression>.Instance);
		static internal Expression Get(params Expression[] terms)
		{
			if(terms.Length == 0)
				return 0.0;

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
			for(int k=0; k<Terms.Length; k++)
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

		protected override Expression Simplify(VisitCache cache)
		{
			Dictionary<Expression, double> newTerms = new Dictionary<Expression, double>();
			double constant = 0.0;

			Action<Expression> addTerm = delegate(Expression e)
			{
				if(e.IsConstant)
					constant += e.Value;
				else
				{
					CoefficientExpression ce = e as CoefficientExpression;
					if(null != ce)
					{
						double val;
						if(!newTerms.TryGetValue(ce.Expression, out val))
							val = 0.0;
						val += ce.Coefficient;
						if(val == 0.0)
							newTerms.Remove(ce.Expression);
						else
							newTerms[ce.Expression] = val;
					}
					else
					{
						double val;
						if(!newTerms.TryGetValue(e, out val))
							val = 0.0;
						val += 1.0;
						if(val == 0.0)
							newTerms.Remove(e);
						else
							newTerms[e] = val;
					}
				}
			};

			foreach(var t in Terms)
			{
				var sf = cache[t];
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
			if(constant != 0.0)
				ts.Add(constant);
			foreach(var kv in newTerms)
				if(kv.Value != 1.0)
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
				if( /*!(ce != null && ce.Coefficient < 0.0)&& */ !(Terms[k].IsConstant && Terms[k].Value < 0.0))
					if(!first)
						sb.Append("+");
				sb.Append(Terms[k].ToString());
				first = false;
			}
			sb.Append(")");
			return sb.ToString();
		}

		public override string ToCode()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("(");
			bool first = true;
			for(int k = 0; k < Terms.Length; k++)
			{
				CoefficientExpression ce = Terms[k] as CoefficientExpression;
				if(/*!(ce != null && ce.Coefficient < 0.0) &&*/ !(Terms[k].IsConstant && Terms[k].Value < 0.0))
					if(!first)
						sb.Append("+");
				sb.Append(Terms[k].ToCode());
				first = false;
			}
			sb.Append(")");
			return sb.ToString();
		}
	}

	public class CoefficientExpression : Expression
	{
		public readonly double Coefficient;
		public readonly Expression Expression;

		//class Comparer : IEqualityComparer<CoefficientExpression>
		//{
		//   public bool Equals(CoefficientExpression x, CoefficientExpression y)
		//   {
		//      return x.Coefficient == y.Coefficient && x.Expression == y.Expression;
		//   }

		//   public int GetHashCode(CoefficientExpression obj)
		//   {
		//      return obj.GetHashCode();
		//   }
		//}

		static Dictionary<Tuple<double, Expression>, CoefficientExpression> cache = new Dictionary<Tuple<double, Expression>, CoefficientExpression>();
		public static Expression Get(double coefficient, Expression expression)
		{
			if(coefficient == 1.0)
				return expression;

			if(expression.IsConstant)
				return coefficient * expression.Value;

			var key = Tuple.Create(coefficient, expression);
			CoefficientExpression e;
			if(!cache.TryGetValue(key, out e))
				cache[key] = e = new CoefficientExpression(coefficient, expression);
			return e;
		}

		protected CoefficientExpression(double coefficient, Expression expression)
		{
			this.Coefficient = coefficient;
			this.Expression = expression;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			var ce = cache[this.Expression];
			if(ce != this.Expression)
				return Get(Coefficient, ce);
			else
				return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			var d = cache[this.Expression];
			if(d.IsZero)
				return d;
			else
				return Get(Coefficient, d);
		}

		protected override Expression Simplify(VisitCache cache)
		{
			if(Coefficient == 0.0)
				return Expression.Zero;

			var e = cache[this.Expression];
			if(e.IsConstant)
				return Coefficient * e.Value;

			CoefficientExpression ce = e as CoefficientExpression;
			if(ce != null)
				return Get(Coefficient * ce.Coefficient, ce.Expression);

			SumExpression se = e as SumExpression;
			if(se != null)
				return SumExpression.Get((from term in se.Terms select Get(Coefficient, term)).ToArray());

			return Get(Coefficient, e);
		}

		public override string ToString()
		{
			if(Coefficient == -1.0)
				return "(-" + Expression.ToString() + ")";
			else
				return "(" + Coefficient.ToString() + " " + Expression.ToString() + ")";
		}

		public override string ToCode()
		{
			if(Coefficient == -1.0)
				return "(-" + Expression.ToCode() + ")";
			else
				return "(" + Coefficient.ToString() + "d*" + Expression.ToCode() + ")";
		}
	}
	
	internal class ProductExpression : Expression
	{
		public readonly Expression[] Factors;

		static Dictionary<Expression[], ProductExpression> productExpressions = new Dictionary<Expression[], ProductExpression>(ReferenceTypeArrayEqualityComparer<Expression>.Instance);
		static internal Expression Get(params Expression[] terms)
		{
			if(terms.Length == 0)
				return 1.0;

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

		protected override Expression Simplify(VisitCache cache)
		{
			Dictionary<Expression, Expression> newFactors = new Dictionary<Expression, Expression>();
			double coefficient = 1.0;

			Action<Expression> addFactor = delegate(Expression e)
			{
				if(e.IsConstant)
					coefficient *= e.Value;
				else
				{
					ApplyExpression ae = e as ApplyExpression;
					if(null != ae && ae.f == Expression.Pow)
					{
						Expression p;
						if(!newFactors.TryGetValue(ae.x[0], out p))
							p = 0.0;
						p += ae.x[1];
						newFactors[ae.x[0]] = p;
					}
					else
					{
						Expression p;
						if(!newFactors.TryGetValue(e, out p))
							p = 0.0;
						p += 1.0;
						newFactors[e] = p;
					}
				}
			};

			foreach(var t in Factors)
			{
				var sf = cache[t];
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
				var p = cache[kv.Value];
				if(p.IsConstant)
				{
					if(kv.Key.IsConstant)
						coefficient *= Math.Pow(kv.Key.Value, p.Value);
					else if(p.Value == 1.0)
						fs.Add(kv.Key);
					else if(p.Value != 0.0)
						fs.Add(Expression.Pow[kv.Key, p]);
				}
				else
					fs.Add(Expression.Pow[kv.Key, p]);
			}
			if(coefficient == 1.0)
				return Get(fs.ToArray());
			else
				return cache[CoefficientExpression.Get(coefficient, Get(fs.ToArray()))];
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

		public override string ToCode()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("(");
			bool first = true;
			for(int k = 0; k < Factors.Length; k++)
			{
				if(!first)
					sb.Append("*");
				sb.Append(Factors[k].ToCode());
				first = false;
			}
			sb.Append(")");
			return sb.ToString();
		}
	}

	public abstract class Function
	{
		public abstract int Arity
		{
			get;
		}

		//public Function Create(Symbol[] arguments, Expression body)
		//{
		//   return new ExpressionFunction(arguments, body);
		//}

		public abstract double Evaluate(params double[] x);

		public abstract Expression PartialDerivative(int i, params Expression[] x);

		static Dictionary<Function, Dictionary<Expression[], Expression>> applyExpressions = new Dictionary<Function, Dictionary<Expression[], Expression>>();

		public virtual Expression this[params Expression[] x]
		{
			get
			{
				if(x.Length != Arity)
					throw new Exception("Wrong number of arguments.");

				Dictionary<Expression[], Expression> d;
				if(!applyExpressions.TryGetValue(this, out d))
					applyExpressions[this] = d = new Dictionary<Expression[], Expression>(ReferenceTypeArrayEqualityComparer<Expression>.Instance);
				Expression e;
				if(!d.TryGetValue(x, out e))
					d[x] = e = new ApplyExpression(this, x);
				return e;
			}
		}

		public virtual Expression TrySimplify(params Expression[] x)
		{
			return null; //	this[x];
		}

		public abstract string ToString(Expression[] x);

		public abstract string ToCode(Expression[] x);
	}

	public class ExpressionFunction : Function
	{
		public readonly Symbol[] Arguments;
		public readonly Expression Body;

		public ExpressionFunction(Symbol[] arguments, Expression body)
		{
			Arguments = arguments;
			Body = body;
			//partials = new ExpressionFunction[arguments.Length];
			//for(int k = 0; k < arguments.Length; k++)
			//   partials[k] = new ExpressionFunction(arguments, body.Derive(arguments[k]).Simplify());
		}

		public override int Arity
		{
			get
			{
				return Arguments.Length;
			}
		}

		public override double Evaluate(params double[] x)
		{
			throw new NotImplementedException();
		}

		public override Expression PartialDerivative(int i, params Expression[] x)
		{
			throw new NotImplementedException();
		}

		public override Expression this[params Expression[] x]
		{
			get
			{
				ExpressionSubstitution[] subs = new ExpressionSubstitution[Arguments.Length];
				for(int k = 0; k < Arguments.Length; k++)
					subs[k] = Arguments[k] | x[k];
				return Body.Substitute(subs);
			}
		}

		public override string ToString(Expression[] x)
		{
			return "((" + string.Join(", ", from a in Arguments select a.ToString()) + ") => " + Body.ToString() + ")";
		}

		public override string ToCode(Expression[] x)
		{
			return "((" + string.Join(", ", from a in Arguments select a.ToString()) + ") => " + Body.ToCode() + ")";
		}
	}

	public class DelegateFunction : Function
	{
		public delegate double EvaluateDelegate(double[] x);
		public delegate Expression PartialDerivativeDelegate(Expression[] x);
		public delegate Expression SimplifyDelegate(Expression[] x);
		public delegate string StringifyDelegate(Expression[] x);
		public delegate string CodifyDelegate(Expression[] x);

		protected EvaluateDelegate evaluate;
		protected PartialDerivativeDelegate[] partials;
		public SimplifyDelegate Simplify;
		public StringifyDelegate Stringify;
		public CodifyDelegate Codify;

		public void SetPartial(int i, PartialDerivativeDelegate partial)
		{
			this.partials[i] = partial;
		}

		public DelegateFunction(string name, string code, EvaluateDelegate evaluate, params PartialDerivativeDelegate[] partials)
		{
			this.Name = name;
			this.Code = code;
			this.evaluate = evaluate;
			//this.Simplify = Simplify;
			this.partials = partials == null ? new PartialDerivativeDelegate[1] : partials;
		}

		public override int Arity
		{
			get
			{
				return partials.Length;
			}
		}

		public string Name
		{
			get;
			protected set;
		}

		public string Code
		{
			get;
			protected set;
		}

		public override Expression PartialDerivative(int i, params Expression[] x)
		{
			return partials[i](x);
		}

		public override double Evaluate(params double[] x)
		{
			return evaluate(x);
		}

		public override Expression TrySimplify(params Expression[] x)
		{
			if(Simplify != null)
			{
				Expression e = Simplify(x);
				if(null != e)
					return e;
			}
			return base.TrySimplify(x);
		}

		public override string ToString(Expression[] x)
		{
			if(Stringify != null)
				return Stringify(x);
			else
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(Name).Append("(");
				for(int k=0; k<x.Length; k++)
					sb.Append(k == 0 ? "" : ", ").Append(x[k].ToString());
				sb.Append(")");
				return sb.ToString();
			}
		}

		public override string ToCode(Expression[] x)
		{
			if(Codify != null)
				return Codify(x);
			else
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(Code).Append("(");
				for(int k = 0; k < x.Length; k++)
					sb.Append(k == 0 ? "" : ", ").Append(x[k].ToCode());
				sb.Append(")");
				return sb.ToString();
			}
		}
	}

	internal class ApplyExpression : Expression
	{
		public Function f;
		public Expression[] x;

		public ApplyExpression(Function f, Expression[] x)
		{
			this.f = f;
			this.x = x;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			//var df = cache[f];
			Expression[] dx = x;
			for(int k = 0; k < x.Length; k++)
				if(x[k] != cache[x[k]])
					dx = new Expression[x.Length];
			if(dx != x)
				for(int k = 0; k < x.Length; k++)
					dx[k] = cache[x[k]];
			//if(f != df || x != dx)
			if(x != dx)
				return f[dx];
			else
				return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			List<Expression> terms = new List<Expression>();
			for(int k = 0; k < x.Length; k++)
			{
				var dx = cache[x[k]];
				if(!dx.IsZero)
					terms.Add(dx * f.PartialDerivative(k, x));
			}
			if(terms.Count == 0)
				return Expression.Zero;
			else if(terms.Count == 1)
				return terms[0];
			else
			{
				terms.Sort();
				return SumExpression.Get(terms.ToArray());
			}
		}

		protected override Expression Simplify(VisitCache cache)
		{
			//var df = cache[f];
			bool allConstant = true;
			Expression[] dx = new Expression[x.Length];
			for(int k = 0; k < x.Length; k++)
			{
					dx[k] = cache[x[k]];
					if(!dx[k].IsConstant)
						allConstant = false;
			}
			if(allConstant)
				return f.Evaluate((from dxx in dx select dxx.Value).ToArray());
			else
			{
				var df = f.TrySimplify(dx);
				if(df != null)
					return cache[df];
				else
					return f[dx];
			}
		}

		public override string ToString()
		{
			return f.ToString(x);
		}

		public override string ToCode()
		{
			return f.ToCode(x);
		}
	}

	internal class AccessExpression : Expression
	{
		public Expression Object;
		public Expression[] Indices;

		static WeakLazyMapping<Expression, Expression[], AccessExpression> instances = new WeakLazyMapping<Expression, Expression[], AccessExpression>(
			(obj, indices) => new AccessExpression(obj, indices),
			null,
			null,
			ReferenceTypeArrayEqualityComparer<Expression>.Instance
			);

		public static Expression Get(Expression obj, Expression[] indices)
		{
			return instances[obj, indices];
		}

		AccessExpression(Expression obj, Expression[] indices)
		{
			this.Object = obj;
			this.Indices = indices;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			var df = cache[Object];
			Expression[] dx = Indices;
			for(int k = 0; k < Indices.Length; k++)
				if(Indices[k] != cache[Indices[k]])
					dx = new Expression[Indices.Length];
			if(dx != Indices)
				for(int k = 0; k < Indices.Length; k++)
					dx[k] = cache[Indices[k]];
			if(Indices != dx || df != Object)
				return df[dx];
			else
				return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			return (s == this) ? 1.0 : 0.0;
		}

		protected override Expression Simplify(VisitCache cache)
		{
			var df = cache[Object];
			Expression[] dx = Indices;
			for(int k = 0; k < Indices.Length; k++)
				if(Indices[k] != cache[Indices[k]])
					dx = new Expression[Indices.Length];
			if(dx != Indices)
				for(int k = 0; k < Indices.Length; k++)
					dx[k] = cache[Indices[k]];

			var oce = df as ObjectConstantExpression;
			if(null != oce)
			{
				FieldExpression fie;
				if(dx.Length == 1 && null != (fie = dx[0] as FieldExpression))
					return Expression.Constant(oce.Object.GetType().GetField(fie.FieldName).GetValue(oce.Object));
				else
				{
					throw new NotImplementedException();
					//var indexer = oce.Object.GetType().GetProperty("Item");
					//indexer.GetValue(oce.Object, (from i in 
				}
			}

			if(Indices == dx && df == Object)
				return this;
			else
				return df[dx];
		}

		public override string ToString()
		{
			if(Indices.Length == 1 && Indices[0] is FieldExpression)
				return Object.ToString() + "." + Indices[0].ToString();
			else
				return Object.ToString() + "[" + string.Join(", ", (from i in Indices select i.ToString())) + "]";
		}

		public override string ToCode()
		{
			if(Indices.Length == 1 && Indices[0] is FieldExpression)
				return Object.ToCode() + "." + Indices[0].ToCode();
			else
				return Object.ToCode() + "[" + string.Join(", ", (from i in Indices select "(int)" + i.ToCode())) + "]";
		}
	}

	//public class PowExpression : Expression
	//{
	//   Expression f;
	//   Expression g;

	//   public PowExpression(Expression f, Expression g)
	//   {
	//      this.f = f;
	//      this.g = g;
	//   }

	//   protected override Expression Substitute(VisitCache cache)
	//   {
	//      var cf = cache[f];
	//      var cg = cache[g];
	//      if(cf != f || cg != g)
	//         return new PowExpression(cf, cg);
	//      else
	//         return this;
	//   }

	//   protected override Expression Derive(VisitCache cache, Symbol s)
	//   {
	//      var df = cache[f];
	//      var dg = cache[g];
	//      return g * df * Expression.Pow(f, g - 1.0) + dg * Expression.Log(f) * this;
	//   }

	//   protected override Expression Simplify(VisitCache cache)
	//   {
	//      var df = cache[f];
	//      var dg = cache[g];
	//      if(df.IsConstant && dg.IsConstant)
	//         return Math.Pow(df.Value, dg.Value);
	//      else if(df.IsZero)
	//         return Expression.Zero;
	//      else if(dg.IsZero)
	//         return Expression.One;
	//      else if(df != f || dg != g)
	//         return new PowExpression(df, dg);
	//      else
	//         return this;
	//   }

	//   public override string ToString()
	//   {
	//      return "Math.Pow(" + f.ToString() + ", " + g.ToString() + ")";
	//   }
	//}

	//public class LogExpression : Expression
	//{
	//   Expression f;

	//   public LogExpression(Expression f)
	//   {
	//      this.f = f;
	//   }

	//   protected override Expression Substitute(VisitCache cache)
	//   {
	//      var cf = cache[f];
	//      if(cf != f)
	//         return new LogExpression(cf);
	//      else
	//         return this;
	//   }

	//   protected override Expression Derive(VisitCache cache, Symbol s)
	//   {
	//      var df = cache[f];
	//      return df / f;
	//   }

	//   protected override Expression Simplify(VisitCache cache)
	//   {
	//      var df = cache[f];
	//      if(df.IsConstant)
	//         return Math.Log(df.Value);
	//      else if(df != f)
	//         return new LogExpression(df);
	//      else
	//         return this;
	//   }

	//   public override string ToString()
	//   {
	//      return "Math.Log(" + f.ToString() + ")";
	//   }
	//}

	//public class ExpExpression : Expression
	//{
	//   Expression f;

	//   public ExpExpression(Expression f)
	//   {
	//      this.f = f;
	//   }

	//   protected override Expression Substitute(VisitCache cache)
	//   {
	//      var cf = cache[f];
	//      if(cf != f)
	//         return new ExpExpression(cf);
	//      else
	//         return this;
	//   }

	//   protected override Expression Derive(VisitCache cache, Symbol s)
	//   {
	//      var df = cache[f];
	//      return df * this;
	//   }

	//   protected override Expression Simplify(VisitCache cache)
	//   {
	//      var df = cache[f];
	//      if(df.IsConstant)
	//         return Math.Exp(df.Value);
	//      else if(df != f)
	//         return new ExpExpression(df);
	//      else
	//         return this;
	//   }

	//   public override string ToString()
	//   {
	//      return "Math.Exp(" + f.ToString() + ")";
	//   }
	//}

	//public class CosExpression : Expression
	//{
	//   Expression f;

	//   public CosExpression(Expression f)
	//   {
	//      this.f = f;
	//   }

	//   protected override Expression Substitute(VisitCache cache)
	//   {
	//      var cf = cache[f];
	//      if(cf != f)
	//         return new CosExpression(cf);
	//      else
	//         return this;
	//   }

	//   protected override Expression Derive(VisitCache cache, Symbol s)
	//   {
	//      var df = cache[f];
	//      return df * Expression.Sin(f);
	//   }

	//   protected override Expression Simplify(VisitCache cache)
	//   {
	//      var df = cache[f];
	//      if(df.IsConstant)
	//         return Math.Cos(df.Value);
	//      else if(df != f)
	//         return new CosExpression(df);
	//      else
	//         return this;
	//   }

	//   public override string ToString()
	//   {
	//      return "Math.Cos(" + f.ToString() + ")";
	//   }
	//}

	//public class SinExpression : Expression
	//{
	//   Expression f;

	//   public SinExpression(Expression f)
	//   {
	//      this.f = f;
	//   }

	//   protected override Expression Substitute(VisitCache cache)
	//   {
	//      var cf = cache[f];
	//      if(cf != f)
	//         return new SinExpression(cf);
	//      else
	//         return this;
	//   }

	//   protected override Expression Derive(VisitCache cache, Symbol s)
	//   {
	//      var df = cache[f];
	//      return -df * Expression.Cos(f);
	//   }

	//   protected override Expression Simplify(VisitCache cache)
	//   {
	//      var df = cache[f];
	//      if(df.IsConstant)
	//         return Math.Sin(df.Value);
	//      else if(df != f)
	//         return new SinExpression(df);
	//      else
	//         return this;
	//   }

	//   public override string ToString()
	//   {
	//      return "Math.Sin(" + f.ToString() + ")";
	//   }
	//}
}
