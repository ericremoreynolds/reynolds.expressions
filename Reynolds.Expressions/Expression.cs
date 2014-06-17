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
			cache.Add(s, 1);
			return cache[this];
		}
		protected abstract Expression Derive(VisitCache cache, Expression s);

		public virtual Expression Normalize(Expression[] arguments)
		{
			return null;
		}

		protected abstract Expression Substitute(VisitCache cache);

		public virtual bool IsScalar
		{
			get
			{
				return false;
			}
		}

		public virtual bool IsNegative
		{
			get
			{
				return false;
			}
		}

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
			return SumExpression.Get(f, -1*g);
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
			return -1 * f;//	CoefficientExpression.Get(-1, f);
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

		public virtual bool GetIsScalar(Expression[] arguments)
		{
			throw new NotImplementedException();
		}
	}
}
