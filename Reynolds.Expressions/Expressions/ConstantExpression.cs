using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Mappings;
using System.Reflection;

namespace Reynolds.Expressions.Expressions
{
	internal class ConstantDoubleExpression : Expression
	{
		static WeakLazyMapping<double, ConstantDoubleExpression> constants = new WeakLazyMapping<double, ConstantDoubleExpression>(value => new ConstantDoubleExpression(value));

		public static Expression Get(double value)
		{
			return constants[value];
		}

		double value;

		public ConstantDoubleExpression(double value)
		{
			this.value = value;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			return 0;
		}

		protected override Expression Normalize(INormalizeContext context)
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

		public override string ToString()
		{
			return value.ToString();
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit(value).Emit("d");
		}
	}

	internal class ConstantIntExpression : Expression
	{
		static WeakLazyMapping<int, ConstantIntExpression> constants = new WeakLazyMapping<int, ConstantIntExpression>(value => new ConstantIntExpression(value));

		public static Expression Get(int value)
		{
			return constants[value];
		}

		int value;

		public ConstantIntExpression(int value)
		{
			this.value = value;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			return 0;
		}

		protected override Expression Normalize(INormalizeContext context)
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

		public override string ToString()
		{
			return value.ToString();
		}

		public override void GenerateCode(ICodeGenerationContext context)
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
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			return 0;
		}

		protected override Expression Normalize(INormalizeContext context)
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
			return obj.ToString();
		}

		public override void GenerateCode(ICodeGenerationContext context)
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

		protected override Expression Normalize(INormalizeContext context, Expression[] arguments)
		{
			if(arguments.All(a => a.IsConstant))
			{
				FieldExpression fie;
				if(arguments.Length == 1 && null != (fie = arguments[0] as FieldExpression))
					//return Expression.Constant(obj.GetType().GetField(fie.FieldName).GetValue(obj));
					return Expression.Constant(obj.GetType().InvokeMember(fie.FieldName, BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance, null, obj, null));
				else
				{
					Type type = obj.GetType();
					if(type.IsArray)
					{
						var arr = obj as Array;
						return Expression.Constant(arr.GetValue(arguments.Select(x => Convert.ToInt32((object) x.Value)).ToArray()), type.GetElementType());
					}
					else
					{
						// TODO: proper type management
						return Expression.Constant(type.InvokeMember(
							"",
							BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty,
							null,
							obj,
							arguments.Select(x => (object) x.Value).ToArray()
							));
					}
				}
			}
			else
			{
				return this[arguments];
			}
		}
	}
}
