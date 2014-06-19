using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Expressions.Expressions;
using System.Reflection;
using Reynolds.Mappings;

namespace Reynolds.Expressions
{
	public abstract class Domain
	{
		public abstract Type Type
		{
			get;
		}

		protected virtual Domain Apply(Expression argument)
		{
			return null;
		}

		protected virtual Domain ApplyTo(Expression target)
		{
			return null;
		}

		protected virtual Domain ApplyCommutative(Expression other)
		{
			return null;
		}

		public static Domain Apply(Expression target, Expression argument)
		{
			Domain d = target.Domain.Apply(argument);
			if(d == null)
				d = argument.Domain.ApplyTo(target);
			if(d == null)
				d = target.Domain.ApplyCommutative(argument);
			if(d == null)
				d = argument.Domain.ApplyCommutative(target);
			if(d == null)
				throw new NotImplementedException();
			return d;
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

		protected virtual Domain Sum(Domain other)
		{
			return null;
		}

		public static Domain Sum(Domain a, Domain b)
		{
			Domain d = a.Sum(b);
			if(d == null)
				d = b.Sum(a);
			if(d == null)
				throw new NotImplementedException();
			return d;
		}

		protected virtual bool IsAssociative(Domain second, Domain third)
		{
			return false;
		}

		protected virtual bool IsCommutative(Domain other)
		{
			return false;
		}

		public static bool AreCommutative(Domain a, Domain b)
		{
			return a.IsCommutative(b); // || b.IsCommutative(a);
		}

		public static bool AreAssociative(Domain a, Domain b, Domain c)
		{
			return a.IsAssociative(b, c); // || b.IsAssociative(a, c) || c.IsAssociative(a, b);
		}

		protected virtual bool IsContainedIn(Domain other)
		{
			return false;
		}

		protected virtual bool Contains(Domain other)
		{
			return false;
		}

		public static readonly Domain Reals = RealsDomain.Instance;
		public static readonly Domain Integers = IntegersDomain.Instance;
		public static readonly Domain Matrices = MatricesDomain.Instance;

		public static readonly Domain Fields = FieldDomain.Instance;

		//public static bool operator < (Domain a, Domain b)
		//{
		//   return a.IsContainedIn(b) || b.Contains(a);
		//}

		//public static bool operator >(Domain b, Domain a)
		//{
		//   return a.IsContainedIn(b) || b.Contains(a);
		//}

		public static bool operator <=(Domain a, Domain b)
		{
			return a == b || a.IsContainedIn(b) || b.Contains(a);
		}

		public static bool operator >=(Domain b, Domain a)
		{
			return a == b || a.IsContainedIn(b) || b.Contains(a);
		}
	}

	public class MatricesDomain : Domain
	{
		protected MatricesDomain()
		{
		}

		public override Type Type
		{
			get
			{
				return null;
			}
		}

		protected override bool IsAssociative(Domain second, Domain third)
		{
			return second <= Domain.Matrices && third <= Domain.Matrices;
		}

		public static readonly MatricesDomain Instance = new MatricesDomain();
	}

	public class RealsDomain : MatricesDomain
	{
		protected RealsDomain()
		{
		}

		public override Type Type
		{
			get
			{
				return typeof(double);
			}
		}

		protected override Domain Apply(Expression argument)
		{
			if(argument.Domain <= Domain.Reals)
				return Domain.Reals;
			else
				return null;
		}

		protected override Domain Sum(Domain other)
		{
			if(other <= Domain.Reals)
				return Domain.Reals;
			return null;
		}

		protected override bool IsCommutative(Domain other)
		{
			return other <= Domain.Reals;
		}

		protected override bool IsContainedIn(Domain other)
		{
			return other <= Domain.Matrices;
		}

		public static new readonly RealsDomain Instance = new RealsDomain();
	}

	public class IntegersDomain : RealsDomain
	{
		protected IntegersDomain()
		{
		}

		public override Type Type
		{
			get
			{
				return typeof(int);
			}
		}

		protected override Domain Apply(Expression argument)
		{
			if(argument.Domain <= Domain.Integers)
				return Domain.Integers;
			else
				return base.Apply(argument);
		}

		protected override Domain Sum(Domain other)
		{
			if(other <= Domain.Integers)
				return Domain.Integers;
			else
				return base.Sum(other);
		}

		protected override bool IsContainedIn(Domain other)
		{
			return other >= Domain.Reals;
		}

		public static new readonly IntegersDomain Instance = new IntegersDomain();
	}

	public class FieldDomain : Domain
	{
		public override Type Type
		{
			get
			{
				return null;
			}
		}

		protected FieldDomain()
		{
		}

		public static readonly FieldDomain Instance;
	}

	public class TypeDomain : Domain
	{
		static WeakLazyMapping<Type, TypeDomain> instances = new WeakLazyMapping<Type, TypeDomain>(t => new TypeDomain(t));

		public static new TypeDomain Get(Type type)
		{
			return instances[type];
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

		protected override Domain Apply(Expression argument)
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
