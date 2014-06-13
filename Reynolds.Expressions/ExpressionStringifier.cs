using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions
{
	public enum StringifyOperator
	{
		None = 0,
		Sum = 1,
		Product = 2,
		Exponent = 3,
		Application = 4
	}

	public interface IStringifyContext
	{
		StringifyOperator EnclosingOperator
		{
			get;
		}

		IStringifyContext Emit(Expression e, StringifyOperator enclosingOperator = StringifyOperator.None);
		IStringifyContext Emit(double d);
		IStringifyContext Emit(int i);
		IStringifyContext Emit(string s);
		IStringifyContext Emit(object o);
		IStringifyContext Emit(Expression e, Expression[] args);
	}

	public class StringifyContext : IStringifyContext
	{
		protected StringBuilder sb = new StringBuilder();
		protected List<StringifyOperator> precedence = new List<StringifyOperator>();

		public StringifyOperator EnclosingOperator
		{
			get;
			protected set;
		}

		public override string ToString()
		{
			return sb.ToString();
		}

		public IStringifyContext Emit(Expression e, StringifyOperator enclosingOperator = StringifyOperator.None)
		{
			var oldOp = EnclosingOperator;
			EnclosingOperator = enclosingOperator;
			e.ToString(this);
			EnclosingOperator = oldOp;
			return this;
		}

		public IStringifyContext Emit(double d)
		{
			sb.Append(d);
			return this;
		}

		public IStringifyContext Emit(int i)
		{
			sb.Append(i);
			return this;
		}

		public IStringifyContext Emit(string s)
		{
			sb.Append(s);
			return this;
		}

		public IStringifyContext Emit(object o)
		{
			sb.Append(o);
			return this;
		}

		public IStringifyContext Emit(Expression e, Expression[] args)
		{
			e.ToString(this, args);
			return this;
		}

		public StringifyOperator EnclosingPrecedence
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public void PushPrecedence(StringifyOperator precedence)
		{
			throw new NotImplementedException();
		}

		public void PopPrecedence()
		{
			throw new NotImplementedException();
		}
	}
}
