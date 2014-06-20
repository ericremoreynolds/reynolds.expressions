using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Mappings;

namespace Reynolds.Expressions.Expressions
{
	public class ApplicationExpression : Expression
	{
		public readonly Expression Target;
		public readonly Expression Argument;

		static WeakLazyMapping<Expression, Expression, ApplicationExpression> instances = new WeakLazyMapping<Expression, Expression, ApplicationExpression>((obj, indices) => new ApplicationExpression(obj, indices));

		protected ApplicationExpression(Expression target, Expression argument)
		{
			this.Target = target;
			this.Argument = argument;

			this.Domain = Domain.Apply(target, argument);
		}

		protected override Expression Substitute(VisitCache cache)
		{
			var df = cache[Target];
			var dx = cache[Argument];

			return df[dx];
		}

		public override Expression Derive(Expression[] arguments, Expression s)
		{
			//var df = Target.Derive(s);
			//if(df != null)
			//   return df.

			//List<Expression> args = new List<Expression>();
			//var df = Target.Derive(Expression.EmptyArguments, s);
			//for(int k = 0; k < arguments.Length + 1; k++)
			//{
			//   if(df != null)
			//      break;

				
			//}

			var args = new Expression[arguments.Length + 1];
			args[0] = Argument;
			Array.Copy(arguments, 0, args, 1, arguments.Length);
			return Target.Domain.Derive(Target, args, s);
		}

		public override void ToString(IStringifyContext context)
		{
			//context.Emit(Target, new Expression[] { Argument });

			context.Emit(Target).Emit(" ").Emit(Argument);
		}

		protected static bool ComesBefore(Expression a, Expression b)
		{
			ApplicationExpression ae;
			if(null != (ae = a as ApplicationExpression))
				return ComesBefore(ae.Argument.GetElement(0), b);
			else if(null != (ae = b as ApplicationExpression))
				return ComesBefore(a, ae.Argument.GetElement(0));

			if(a.IsConstant && a.Domain <= Domain.Reals)
			{
				if(b.IsConstant && b.Domain <= Domain.Reals)
					return a.Value.CompareTo(b.Value) < 0;
				else
					return true;
			}
			else
			{
				if(b.IsConstant && b.Domain <= Domain.Reals)
					return false;
				else
					return a.CompareTo(b) < 0;
			}
		}

		public override Expression Apply(params Expression[] arguments)
		{
			var args = new Expression[arguments.Length + 1];
			args[0] = Argument;
			Array.Copy(arguments, 0, args, 1, arguments.Length);
			return Target.Apply(args);
		}

		public static Expression Get(Expression target, Expression argument)
		{
			ApplicationExpression ae;
			
			while(null != (ae = argument as ApplicationExpression) && Domain.AreAssociative(target.Domain, ae.Target.Domain, ae.Argument.Domain))
			{
				if(Domain.AreCommutative(target.Domain, ae.Target.Domain) && ComesBefore(ae.Target, target))
				{
					target = ae.Target.Apply(target);
					argument = ae.Argument;
				}
				else
				{
					target = target.Apply(ae.Target);
					argument = ae.Argument;
				}
			}

			if(null != (ae = target as ApplicationExpression) && Domain.AreCommutative(ae.Argument.Domain, argument.Domain))
			{
				if(ComesBefore(argument, ae.Argument))
				{
					target = ae.Target.Apply(argument);
					argument = ae.Argument;
				}
			}
			else
			{
				if(Domain.AreCommutative(target.Domain, argument.Domain) && ComesBefore(argument, target))
					return argument.Apply(target);
			}

			if(target == argument)
				return Expression.Pow[target, 2];
			else if(null != (ae = argument as ApplicationExpression) && ae.Target == Expression.Pow && target == ae.Argument)
			{
				return argument.Apply(target);
			}

			return instances[target, argument];

			//List<Expression> sequence = new List<Expression>();
			//sequence.Add(argument);
			//while(null != (ae = target as ApplicationExpression))
			//{
			//   sequence.Insert(0, ae.Argument);
			//   target = ae.Target;
			//}
			//sequence.Insert(0, target);

			//bool anyChanges = false;
			//bool changes = true;
			//while(changes)
			//{
			//   changes = false;
			//   for(int k=0; k<sequence.Count-1; k++)
			//      if(Domain.AreCommutative(sequence[k].Domain, sequence[k + 1].Domain) && ComesBefore(sequence[k + 1], sequence[k]))
			//      {
			//         var tmp = sequence[k];
			//         sequence[k] = sequence[k + 1];
			//         sequence[k + 1] = tmp;
			//         changes = true;
			//         anyChanges = true;
			//      }
			//}

			//if(anyChanges)
			//{
			//   Expression e = sequence[0];
			//   for(int k = 1; k < sequence.Count; k++)
			//      e = e.Apply(sequence[k]);
			//   return e;
			//}
		}
	}
}
