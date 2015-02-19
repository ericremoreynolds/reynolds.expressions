using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions.Domains
{
	public class RealsDomain : Domain
	{
		public override Domain Base
		{
			get
			{
				return Domain.Matrices;
			}
		}

		public override Type Type
		{
			get
			{
				return typeof(double);
			}
		}

		protected override Domain LeftApply(Expression target, Expression argument)
		{
			if(argument.Domain <= Domain.Reals)
				return Domain.Reals;

			return base.LeftApply(target, argument);
		}

		protected override Domain Sum(Domain other)
		{
			if(other <= Domain.Reals)
				return Domain.Reals;

			return base.Sum(other);
		}

		protected override bool IsCommutative(Domain other)
		{
			return other <= Domain.Reals || base.IsCommutative(other);
		}

		protected static RealsDomain instance;
		public static RealsDomain Instance
		{
			get
			{
				if(instance == null)
					instance = new RealsDomain();
				return instance;
			}
		}
	}
}
