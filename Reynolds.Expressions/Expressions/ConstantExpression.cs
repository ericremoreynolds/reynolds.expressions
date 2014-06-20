using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Mappings;
using System.Reflection;

namespace Reynolds.Expressions.Expressions
{
	public class ConstantDoubleExpression : Expression
	{
		static WeakLazyMapping<double, ConstantDoubleExpression> constants = new WeakLazyMapping<double, ConstantDoubleExpression>(value => new ConstantDoubleExpression(value));

		public static Expression Get(double value)
		{
			return constants[value];
		}

		double value;

		protected ConstantDoubleExpression(double value)
		{
			this.value = value;

			this.Domain = Domain.Reals;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		public override Expression Derive(Expression[] arguments, Expression s)
		{
			return 0;
		}

		public override bool IsConstant
		{
			get
			{
				return true;
			}
		}

		public override bool IsNegative
		{
			get
			{
				return value < 0.0;
			}
		}

		public override bool IsZero
		{
			get
			{
				return value == 0;
			}
		}

		public override bool IsOne
		{
			get
			{
				return value == 1;
			}
		}

		public override object Value
		{
			get
			{
				return value;
			}
		}

		public override Expression Apply(params Expression[] arguments)
		{
			if(arguments.Length == 1)
			{
				if(arguments[0].IsConstant && arguments[0].Domain <= Domain.Reals)
					return value * arguments[0].Value;
			}

			return base.Apply(arguments);
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit(value);
		}

		public override void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
		{
			context.Emit(value).Emit("d");
		}
	}

	public class ConstantIntExpression : Expression
	{
		static WeakLazyMapping<int, ConstantIntExpression> constants = new WeakLazyMapping<int, ConstantIntExpression>(value => new ConstantIntExpression(value));

		public static Expression Get(int value)
		{
			return constants[value];
		}

		int value;

		protected ConstantIntExpression(int value)
		{
			this.value = value;

			Domain = Domain.Integers;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		public override Expression Derive(Expression[] arguments, Expression s)
		{
			return 0;
		}

		public override bool IsConstant
		{
			get
			{
				return true;
			}
		}

		public override bool IsNegative
		{
			get
			{
				return value < 0;
			}
		}

		public override bool IsZero
		{
			get
			{
				return value == 0;
			}
		}

		public override bool IsOne
		{
			get
			{
				return value == 1;
			}
		}

		public override dynamic Value
		{
			get
			{
				return value;
			}
		}

		public override Expression Apply(params Expression[] arguments)
		{
			if(arguments.Length == 1)
			{
				if(arguments[0].IsConstant && arguments[0].Domain <= Domain.Reals)
					return value * arguments[0].Value;
			}

			return base.Apply(arguments);
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit(value);
		}

		public override void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
		{
			context.Emit(value);
		}
	}

	public class ConstantObjectExpression : Expression
	{
		protected object obj;
		protected Type type;

		static WeakLazyMapping<object, Type, ConstantObjectExpression> cache = new WeakLazyMapping<object, Type, ConstantObjectExpression>((o, t) => new ConstantObjectExpression(o, t));
		public static Expression Get(object obj, Type type)
		{
			return cache[obj, type];
		}

		ConstantObjectExpression(object obj, Type type)
		{
			this.obj = obj;
			this.type = type;

			Domain = Domain.Get(type);
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		public override Expression Derive(Expression[] arguments, Expression s)
		{
			return 0;
		}

		public override bool IsConstant
		{
			get
			{
				return true;
			}
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit(obj);
		}

		public override void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
		{
			context.Emit(obj, type);
		}

		public override dynamic Value
		{
			get
			{
				return obj;
			}
		}

		public override Expression Apply(params Expression[] arguments)
		{
			if(arguments.Length == 1)
			{
				var argument = arguments[0];

				FieldExpression fie;
				if(null != (fie = argument as FieldExpression))
				{
					FieldInfo fi = obj.GetType().GetField(fie.FieldName);
					PropertyInfo pi = obj.GetType().GetProperty(fie.FieldName);
					Type t = fi == null ? pi.PropertyType : fi.FieldType;

					//return Expression.Constant(obj.GetType().GetField(fie.FieldName).GetValue(obj));
					return Expression.Constant(obj.GetType().InvokeMember(fie.FieldName, BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance, null, obj, null), t);
				}
				else if(argument.All(a => a.IsConstant))
				{
					// TODO: proper type management
					return Expression.Constant(type.InvokeMember(
						"",
						BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty,
						null,
						obj,
						argument.Select(x => (object) x.Value).ToArray()
						));
				}
			}

			return base.Apply(arguments);
		}
	}
}
