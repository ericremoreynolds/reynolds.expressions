using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions.Domains
{
	public class UniversalDomain : Domain
	{
		protected override bool Contains(Domain other)
		{
			return true;
		}

		protected override bool IsContainedIn(Domain other)
		{
			return false;
		}

		public override Expression Derive(Expression target, Expression[] arguments, Expression s)
		{
			return null;
		}

		protected override Domain LeftApply(Expression target, Expression argument)
		{
			return null;
		}

		protected override Domain Sum(Domain other)
		{
			return null;
		}

		protected override bool IsCommutative(Domain other)
		{
			return false;
		}

		protected override bool IsAssociative(Domain second, Domain third)
		{
			return false;
		}

		public static readonly UniversalDomain Instance = new UniversalDomain();

		public override Domain Base
		{
			get
			{
				return null;
			}
		}
	}
}
