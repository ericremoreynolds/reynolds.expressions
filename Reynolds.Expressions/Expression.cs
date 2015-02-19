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

		public virtual bool IsMatrix
		{
			get
			{
				return false;
			}
		}

		public virtual Expression Rows
		{
			get
			{
				return MatrixRowsExpression.Get(this);
			}
		}

		public virtual Expression Columns
		{
			get
			{
				return MatrixColumnsExpression.Get(this);
			}
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

		public static IDerivativeCache NewDerivativeCache()
		{
			return new DerivativeCache();
		}

		public Expression Derive(Expression variable)
		{
			var cache = new DerivativeCache();
			return cache[this, variable];
		}

		internal abstract Expression Derive(IDerivativeCache cache, Expression variable);

		public virtual Expression Normalize(Expression[] arguments)
		{
			return null;
		}

		protected abstract Expression Substitute(VisitCache cache);

		//public virtual bool IsScalar
		//{
		//   get
		//   {
		//      return false;
		//   }
		//}

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

		public static implicit operator Expression(DateTime dt)
		{
			return ConstantObjectExpression.Get(dt, typeof(DateTime));
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

		public static Expression operator %(Expression f, Expression g)
		{
			return MatrixMultiplyExpression.Get(f, g);
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
		public static readonly FunctionExpression Identity = new IdentityFunction();
		
		public TDelegate Compile<TDelegate>(params Symbol[] xs)
		{
			ExpressionCompiler c = new ExpressionCompiler();
			c.Add(this, typeof(TDelegate), null, (from x in xs select (ExpressionCompilerArgument) x).ToArray());
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

		public static Expression MMult(params Expression[] factors)
		{
			return MatrixMultiplyExpression.Get(factors);
		}

		public virtual Expression Inverse()
		{
			return MatrixInverseExpression.Get(this);
		}

		public virtual Expression Transpose()
		{
			return MatrixTransposeExpression.Get(this);
		}

		public static Expression Sum(params Expression[] terms)
		{
			return SumExpression.Get(terms);
		}

		public static Expression Sum(Expression collection, Func<Symbol, Expression> term)
		{
			return CollectionSumExpression.Get(collection, term);
		}

		public static Expression Sum(Expression from, Expression to, Func<Symbol, Expression> term)
		{
			return DynamicSumExpression.Get(from, to, term);
		}

		public static Expression Product(params Expression[] terms)
		{
			return ProductExpression.Get(terms);
		}

		//public virtual bool GetIsScalar(Expression[] arguments)
		//{
		//   throw new NotImplementedException();
		//}
	}
}
