using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;
using Reynolds.Mappings;
using Reynolds.Expressions.Functions;
using Reynolds.Expressions.Expressions;

namespace Reynolds.Expressions
{
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

		public Expression Normalize()
		{
			VisitCache cache = new VisitCache((f, c) => f.Normalize(c));
			return cache[this];
		}

		protected abstract Expression Substitute(VisitCache cache);
		protected abstract Expression Derive(VisitCache cache, Expression s);
		protected abstract Expression Normalize(VisitCache cache);

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

		public virtual dynamic Value
		{
			get
			{
				throw new Exception("Function is not fully evaluated.");
			}
		}

		public static implicit operator Expression(double a)
		{
			return ConstantDoubleExpression.Get(a);
		}

		public static implicit operator Expression(int a)
		{
			return ConstantIntExpression.Get(a);
		}


		public static Expression operator +(Expression f, Expression g)
		{
			return SumExpression.Get(f, g);
		}

		public static Expression operator -(Expression f, Expression g)
		{
			return SumExpression.Get(f, CoefficientExpression.Get(-1, g));
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
			return CoefficientExpression.Get(-1, f);
		}

		public bool NormalizesTo(object obj)
		{
			var n = this.Normalize();
			var other = obj as Expression;
			if(other != null)
				return other.Normalize() == n;
			if(n.IsConstant)
				return n.Value == (dynamic) obj;
			return false;
		}

		public virtual Expression this[params Expression[] arguments]
		{
			get
			{
				return ApplicationExpression.Get(this, arguments);
			}
		}

		public static readonly FunctionExpression Log = new DelegateFunction(
			"log", "Math.Log",
			x => Math.Log(x[0]),
			x => 1 / x[0]
			);

		public static readonly FunctionExpression Exp = new DelegateFunction(
			"exp", "Math.Exp",
			x => Math.Exp(x[0]),
			x => Exp[x[0]]
			);

		public static readonly FunctionExpression Pow = new DelegateFunction(
			"pow", "Math.Pow",
			x => Math.Pow(x[0], x[1]),
			x => x[1] * Pow[x[0], x[1] - 1],
			x => Log[x[0]] * Pow[x[0], x[1]]
			);

		public static readonly FunctionExpression Sin = new DelegateFunction(
			"sin", "Math.Sin",
			x => Math.Sin(x[0]),
			null // set after
			);

		public static readonly FunctionExpression Cos = new DelegateFunction(
			"cos", "Math.Cos",
			x => Math.Cos(x[0]),
			null // set after
			);
		
		static Expression()
		{
			((DelegateFunction) Sin).SetPartial(0, x => Cos[x[0]]);
			((DelegateFunction) Cos).SetPartial(0, x => -Sin[x[0]]);
			((DelegateFunction) Pow).Stringify = (x) => (x[0].ToString() + "^" + x[1].ToString());

			((DelegateFunction) Pow).Simplify = delegate(Expression[] x)
			{
				var ae = x[0] as ApplicationExpression;
				var pe = x[0] as ProductExpression;
				CoefficientExpression ce;
				if(pe != null)
					return ProductExpression.Get((from f in pe.Factors select Expression.Pow[f, x[1]]).ToArray());
				else if(null != (ce = x[0] as CoefficientExpression))
					return ProductExpression.Get(Expression.Pow[ce.Coefficient, x[1]], Expression.Pow[ce.Expression, x[1]]);
				else if(ae != null && ae.Applicand == Expression.Pow)
					return Expression.Pow[ae.Arguments[0], ae.Arguments[1] * x[1]];
				else
					return null;
			};

			((DelegateFunction) Pow).Codify = delegate(Expression[] x)
			{
				if(x[1].IsConstant && x[1].Value == -1)
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

		public virtual string ToCode(Expression[] arguments)
		{
			throw new NotImplementedException();
		}

		public virtual string ToString(Expression[] arguments)
		{
			throw new NotImplementedException();
		}

		public int CompareTo(Expression other)
		{
			return this.ordinal.CompareTo(other.ordinal);
		}

		public static Expression Constant(object obj)
		{
			if(obj is double)
				return (double) obj;
			else if(obj is int)
				return (int) obj;
			else
				return ConstantObjectExpression.Get(obj);
		}

		public static Expression Field(string name)
		{
			return FieldExpression.Get(name);
		}

		public virtual Expression GetPartialDerivative(int index, Expression[] arguments)
		{
			throw new NotImplementedException();
		}

		public virtual Expression Normalize(Expression[] arguments)
		{
			throw new NotImplementedException();
		}
	}
}
