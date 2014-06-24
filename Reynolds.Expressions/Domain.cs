using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Expressions.Expressions;
using System.Reflection;
using Reynolds.Mappings;
using Reynolds.Expressions.Domains;

namespace Reynolds.Expressions
{
	public abstract class Domain
	{
		public virtual Type Type
		{
			get
			{
				return null;
			}
		}

		public abstract Domain Base
		{
			get;
		}

		protected virtual void InitializeExpressionHook(Expression expression)
		{
		}

		public static void InitializeExpression(Expression expression, params Domain[] domains)
		{
			foreach(var domain in domains)
				for(var d = domain; d != null; d = d.Base)
					d.InitializeExpressionHook(expression);
		}

		protected virtual Expression NormalizeExpressionHook(Expression expression)
		{
			return expression;
		}

		public static Expression NormalizeExpression(Expression expression, params Domain[] domains)
		{
			Expression e = expression;
			foreach(var domain in domains)
				for(var d = domain; d != null; d = d.Base)
					e = d.NormalizeExpressionHook(e);
		}

		public virtual Expression Derive(Expression target, Expression[] arguments, Expression s)
		{
			return Base.Derive(target, arguments, s);
		}

		public static Domain Get(Type type)
		{
			if(type == typeof(double))
				return Reals;
			else if(type == typeof(int))
				return Integers;
			else
				return TypeDomain.Get(type);
		}

		protected virtual bool IsAssociative(Domain second, Domain third)
		{
			return Base.IsAssociative(second, third);
		}

		protected virtual bool IsCommutative(Domain other)
		{
			return Base.IsCommutative(other);
		}

		public static bool AreCommutative(Domain a, Domain b)
		{
			return a.IsCommutative(b) || b.IsCommutative(a);
		}

		public static bool AreAssociative(Domain a, Domain b, Domain c)
		{
			return a.IsAssociative(b, c) || b.IsAssociative(a, c) || c.IsAssociative(a, b);
		}

		protected virtual bool IsContainedIn(Domain other)
		{
			return Base <= other;
		}

		protected virtual bool Contains(Domain other)
		{
			return false;
		}

		public static readonly Domain Universal = UniversalDomain.Instance;

		public static readonly Domain Matrices = MatricesDomain.Instance;
		public static readonly Domain Reals = RealsDomain.Instance;
		public static readonly Domain Integers = IntegersDomain.Instance;

		public static bool operator <=(Domain a, Domain b)
		{
			return a == b || a.IsContainedIn(b) || b.Contains(a);
		}

		public static bool operator >=(Domain b, Domain a)
		{
			return a == b || a.IsContainedIn(b) || b.Contains(a);
		}

		public static Domain operator *(Domain a, Domain b)
		{
			if(a <= b)
				return b;
			else if(b <= a)
				return a;
			else
				// TODO: domain intersections
				throw new NotImplementedException();
		}
	}

	public class RealFunctionsDomain : Domain
	{
		public static readonly RealFunctionsDomain Instance = new RealFunctionsDomain();

		public override Domain Base
		{
			get
			{
				return Domain.Universal;
			}
		}

		protected override void InitializeExpressionHook(Expression expression)
		{
			ApplicationExpression ae;
			if(null != (ae = expression as ApplicationExpression))
				expression.Domain *= Domain.Reals;
		}
	}

	public class PowFunctionDomain : Domain
	{
		protected override void InitializeExpressionHook(Expression expression)
		{
			ApplicationExpression ae;
			if(null != (ae = expression as ApplicationExpression) && ae.Target.Domain <= this)
			{
				if(ae.Argument.GetElement(1).Domain <= Domain.Integers)
					expression.Domain *= ae.Argument.GetElement(0).Domain;
				else if(ae.Argument.GetElement(0).Domain <= Domain.Reals && ae.Argument.GetElement(1).Domain <= Domain.Reals)
					expression.Domain *= Domain.Reals;
			}
		}

		public override Domain Base
		{
			get
			{
				return RealFunctionsDomain.Instance;
			}
		}

		public static readonly PowFunctionDomain Instance = new PowFunctionDomain();
	}

	public class TypeDomain : Domain
	{
		static WeakLazyMapping<Type, TypeDomain> instances = new WeakLazyMapping<Type, TypeDomain>(t => new TypeDomain(t));

		public static new TypeDomain Get(Type type)
		{
			return instances[type];
		}

		public override Domain Base
		{
			get
			{
				return Domain.Universal;
			}
		}

		TypeDomain(Type type)
		{
			this.type = type;
		}

		Type type;
		public override Type Type
		{
			get
			{
				return type;
			}
		}

		protected override Domain LeftApply(Expression target, Expression argument)
		{
			FieldExpression fe = argument as FieldExpression;
			if(null != fe)
			{
				FieldInfo fi = type.GetField(fe.FieldName);
				PropertyInfo pi = type.GetProperty(fe.FieldName);
				Type t = fi == null ? pi.PropertyType : fi.FieldType;
				return Domain.Get(t);
			}
			else
			{
				if(type.IsArray)
				{
					return Domain.Get(type.GetElementType());
				}
				else
				{
					// TODO: proper type management
					var method = Type.DefaultBinder.SelectMethod(
						BindingFlags.Public | BindingFlags.Instance,
						type.GetMethods().Where(m => m.Name == "").ToArray(),
						argument.Select(e => e.Domain.Type).ToArray(),
						null
						);
					var mi = method as MethodInfo;
					return Domain.Get(mi.ReturnType);
				}
			}
		}
	}
}
