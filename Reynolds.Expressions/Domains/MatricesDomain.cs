using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions.Domains
{
	public class MatricesDomain : Domain
	{
		public override Domain Base
		{
			get
			{
				return Domain.Universal;
			}
		}

		public override Type Type
		{
			get
			{
				return null;
			}
		}

		public override Expression Derive(Expression target, Expression[] arguments, Expression s)
		{
			if(arguments.Length == 1)
			{
				var df = target.Derive(Expression.EmptyArguments, s);
				var dg = arguments[0].Derive(Expression.EmptyArguments, s);
				return df * arguments[0] + target * dg;
			}
			else
				return base.Derive(target, arguments, s);
		}

		protected override bool IsAssociative(Domain second, Domain third)
		{
			return second <= Domain.Matrices && third <= Domain.Matrices;
		}

		protected static MatricesDomain instance;
		public static MatricesDomain Instance
		{
			get
			{
				if(instance == null)
					instance = new MatricesDomain();
				return instance;
			}
		}
	}
}
