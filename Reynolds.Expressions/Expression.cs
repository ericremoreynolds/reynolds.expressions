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
using System.CodeDom;

namespace Reynolds.Expressions
{
	public delegate double CompiledExpression(params double[] x);

	public interface INormalizeContext
	{
		Expression Normalize(Expression e);
		Expression Normalize(Expression e, Expression[] arguments);
	}

	public abstract class Expression : IComparable<Expression>
	{
		protected class NormalizeContext : INormalizeContext
		{
			Dictionary<Expression, Expression> cache = new Dictionary<Expression, Expression>();

			public Expression Normalize(Expression e)
			{
				Expression f;
				if(!cache.TryGetValue(e, out f))
					cache[e] = f = e.Normalize(this);
				return f;
			}

			public Expression Normalize(Expression e, Expression[] arguments)
			{
				return e.Normalize(this, arguments);
			}
		}

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
			cache.Add(s, 1);
			return cache[this];
		}
		protected abstract Expression Derive(VisitCache cache, Expression s);

		public Expression Normalized
		{
			get
			{
				NormalizeContext context = new NormalizeContext();
				return context.Normalize(this);
			}
		}
		protected abstract Expression Normalize(INormalizeContext context);
		protected virtual Expression Normalize(INormalizeContext context, Expression[] arguments)
		{
			return this[arguments];
		}
		public static Expression[] Normalize(params Expression[] expressions)
		{
			NormalizeContext context = new NormalizeContext();
			return expressions.Select(e => context.Normalize(e)).ToArray();
		}

		protected abstract Expression Substitute(VisitCache cache);

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
			var n = this.Normalized;
			var other = obj as Expression;
			if(other != null)
				return other.Normalized == n;
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

		public Expression this[params ExpressionSubstitution[] substitutions]
		{
			get
			{
				return this.Substitute(substitutions);
			}
		}

		public static readonly FunctionExpression Log = new LogFunction();
		public static readonly FunctionExpression Exp = new ExpFunction();
		public static readonly FunctionExpression Pow = new PowFunction();
		public static readonly FunctionExpression Sin = new SinFunction();
		public static readonly FunctionExpression Cos = new CosFunction();
		
		//public static MethodInfo[] Compile(Expression[] es, Symbol[] x, Type[] types)
		//{
		//   ExpressionSubstitution[] subs = new ExpressionSubstitution[x.Length];
		//   for(int k = 0; k < x.Length; k++)
		//      subs[k] = x[k] | new Symbol("x" + k.ToString());
		//   es = (from e in es select e.Substitute(subs)).ToArray();

		//   StringBuilder sb = new StringBuilder();
		//   sb.Append("using System;");
		//   sb.Append("public static class GeneratedFunction {");
		//   for(int k = 0; k < es.Length; k++)
		//   {
		//      sb.Append("public static double f" + k.ToString() + "(" + string.Join(", ", (from i in Enumerable.Range(0, x.Length) select types[i].FullName + " x" + i.ToString())) + ") { return " + es[k].ToCode() + "; }");
		//   }
		//   sb.Append("}");
		//   string code = sb.ToString();

		//   CompilerParameters parameters = new CompilerParameters();
		//   parameters.GenerateInMemory = true;
		//   parameters.TreatWarningsAsErrors = false;
		//   parameters.GenerateExecutable = false;
		//   parameters.CompilerOptions = "/optimize";
		//   parameters.IncludeDebugInformation = false;
		//   parameters.ReferencedAssemblies.Add("System.dll");
		//   foreach(var typ in types)
		//      parameters.ReferencedAssemblies.Add(typ.Assembly.Location);

		//   CompilerResults results = new CSharpCodeProvider().CompileAssemblyFromSource(parameters, new string[] { code });

		//   if(results.Errors.HasErrors)
		//      throw new Exception("Compile error: " + results.Errors[0].ToString()); //, results.Errors);

		//   MethodInfo[] ces = new MethodInfo[es.Length];
		//   for(int k = 0; k < es.Length; k++)
		//   {
		//      var mi = results.CompiledAssembly.GetModules()[0].GetType("GeneratedFunction").GetMethod("f" + k.ToString());
		//      ces[k] = mi;
		//   }

		//   return ces;
		//}

		public TDelegate Compile<TDelegate>(params Symbol[] x)
		{
			ExpressionCompiler c = new ExpressionCompiler();
			c.Add(this, typeof(TDelegate), null, x);
			return (TDelegate) (object) c.CompileAll()[0];
		}

		public Func<double, double> Compile(Symbol x0)
		{
			return Compile<Func<double, double>>(x0);
		}

		public Func<double, double, double> Compile(Symbol x0, Symbol x1)
		{
			return Compile<Func<double, double, double>>(x0, x1);
		}


		public Func<double, double, double, double> Compile(Symbol x0, Symbol x1, Symbol x2)
		{
			return Compile<Func<double, double, double, double>>(x0, x1, x2);
		}

		public abstract void GenerateCode(ICodeGenerationContext context);
		public virtual void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
		{
			context.Emit(this);

			FieldExpression fe;
			if(arguments.Length == 1 && null != (fe = arguments[0] as FieldExpression))
				context.Emit(".").Emit(fe.FieldName);
			else
			{
				context.Emit("[");
				for(int k = 0; k < arguments.Length; k++)
				{
					if(k > 0)
						context.Emit(", ");
					context.Emit(arguments[k]);
				}
				context.Emit("]");
			}
		}

		public override string ToString()
		{
			StringifyContext sc = new StringifyContext();
			this.ToString(sc);
			return sc.ToString();
		}

		public abstract void ToString(IStringifyContext context);
		public virtual void ToString(IStringifyContext context, Expression[] arguments)
		{
			context.Emit(this, StringifyOperator.Application);

			FieldExpression fe;
			if(arguments.Length == 1 && null != (fe = arguments[0] as FieldExpression))
				context.Emit(".").Emit(fe.FieldName);
			else
			{
				context.Emit("[");
				for(int k = 0; k < arguments.Length; k++)
				{
					if(k > 0)
						context.Emit(", ");
					context.Emit(arguments[k].ToString());
				}
				context.Emit("]");
			}
		}

		public int CompareTo(Expression other)
		{
			return this.ordinal.CompareTo(other.ordinal);
		}

		public static Expression Constant<T>(T obj)
		{
			return Constant(obj, typeof(T));
		}

		public static Expression Constant(object obj, Type type = null)
		{
			if(type == null)
				type = obj.GetType();

			if(type == typeof(double))
				return (double) obj;
			else if(type == typeof(int))
				return (int) obj;
			else
				return ConstantObjectExpression.Get(obj, type);
		}

		public static Expression Field(string name)
		{
			return FieldExpression.Get(name);
		}

		public static Expression Cast<T>(Expression e)
		{
			return CastExpression.Get(typeof(T), e);
		}

		public virtual Expression GetPartialDerivative(int index, Expression[] arguments)
		{
			throw new NotImplementedException();
		}
	}
}
