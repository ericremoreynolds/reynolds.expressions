using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions.Domains
{
	public class IntegersDomain : Domain
	{
		public override Domain Base
		{
			get
			{
				return Domain.Reals;
			}
		}

		public override Type Type
		{
			get
			{
				return typeof(int);
			}
		}

		protected override Domain LeftApply(Expression target, Expression argument)
		{
			if(argument.Domain <= Domain.Integers)
				return Domain.Integers;

			return base.LeftApply(target, argument);
		}

		protected override Domain Sum(Domain other)
		{
			if(other <= Domain.Integers)
				return Domain.Integers;

			return base.Sum(other);
		}

		public static readonly IntegersDomain Instance = new IntegersDomain();
	}
}
