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

	public abstract class Expression : IComparable<Expression>, IEnumerable<Expression>
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
			//VisitCache cache = new VisitCache((f, c) => f.Derive(c, s));
			//cache.Add(s, 1);
			//return cache[this];

			return Derive(Expression.EmptyArguments, s);
		}
		public virtual Expression Derive(Expression[] arguments, Expression s)
		{
			var dx = Domain.Derive(this, arguments, s);
			if(null != dx)
				return dx;
			//throw new NotImplementedException();
			return null;
		}

		public Domain Domain
		{
			get;
			protected set;
		}

		public static readonly Expression[] EmptyArguments = new Expression[0];

		public virtual Expression Apply(params Expression[] arguments)
		{
			var e = this;
			foreach(var arg in arguments)
				e = ApplicationExpression.Get(e, arg);
			return e;
		}

		protected abstract Expression Substitute(VisitCache cache);

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
			return f[g];
		}

		public static Expression operator /(Expression f, Expression g)
		{
			return f[Expression.Pow[g, -1]];
		}

		public static Expression operator -(Expression f)
		{
			return Expression.Constant(-1)[f];
		}

		//public virtual Expression this[Expression argument]
		//{
		//   get
		//   {
		//      return this.Apply(this, argument);
		//      //return ApplicationExpression.Get(this, argument);
		//   }
		//}

		public Expression this[params Expression[] arguments]
		{
			get
			{
				//return this[TupleExpression.Get(arguments)];
				return this.Apply(TupleExpression.Get(arguments));
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
		public static readonly FunctionExpression Pow = PowFunction.Instance;
		public static readonly FunctionExpression Sin = new SinFunction();
		public static readonly FunctionExpression Cos = new CosFunction();
		
		public TDelegate Compile<TDelegate>(params SymbolExpression[] x)
		{
			ExpressionCompiler c = new ExpressionCompiler();
			c.Add(this, typeof(TDelegate), null, x);
			return (TDelegate) (object) c.CompileAll()[0];
		}

		public virtual void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
		{
			if(arguments.Length == 0)
				throw new NotImplementedException();

			context.Emit(this);

			foreach(var arg in arguments)
			{
				FieldExpression fe;
				if(null != (fe = arg as FieldExpression))
					context.Emit(".").Emit(fe.FieldName);
				else
				{
					bool first = true;
					context.Emit("[");
					foreach(var el in arg)
					{
						if(!first)
							context.Emit(", ");
						context.Emit(el);
						first = false;
					}
					context.Emit("]");
				}
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

		//public static Expression Cast<T>(Expression e)
		//{
		//   return CastExpression.Get(typeof(T), e);
		//}

		public static SymbolExpression Symbol(string name, Domain domain)
		{
			return SymbolExpression.Get(name, domain);
		}

		public static SymbolExpression Symbol<T>(string name)
		{
			return Symbol(name, Domain.Get(typeof(T)));
		}

		//public virtual Expression GetPartialDerivative(int index, Expression[] arguments)
		//{
		//   throw new NotImplementedException();
		//}

		public virtual int Count
		{
			get
			{
				return 1;
			}
		}

		public virtual Expression GetElement(int k)
		{
			if(k != 0)
				throw new IndexOutOfRangeException();
			return this;
		}

		public virtual IEnumerator<Expression> GetEnumerator()
		{
			yield return this;
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((Expression) this).GetEnumerator();
		}
	}
}
