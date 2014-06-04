using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Mappings;

namespace Reynolds.Expressions.Expressions
{
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
}
