using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Mappings;

namespace Reynolds.Expressions.Expressions
{
	internal class AccessExpression : Expression
	{
		public Expression Object;
		public Expression[] Indices;

		static WeakLazyMapping<Expression, Expression[], AccessExpression> instances = new WeakLazyMapping<Expression, Expression[], AccessExpression>(
			(obj, indices) => new AccessExpression(obj, indices),
			null,
			null,
			ReferenceTypeArrayEqualityComparer<Expression>.Instance
			);

		public static Expression Get(Expression obj, Expression[] indices)
		{
			return instances[obj, indices];
		}

		AccessExpression(Expression obj, Expression[] indices)
		{
			this.Object = obj;
			this.Indices = indices;
		}

		protected override Expression Substitute(VisitCache cache)
		{
			var df = cache[Object];
			Expression[] dx = Indices;
			for(int k = 0; k < Indices.Length; k++)
				if(Indices[k] != cache[Indices[k]])
					dx = new Expression[Indices.Length];
			if(dx != Indices)
				for(int k = 0; k < Indices.Length; k++)
					dx[k] = cache[Indices[k]];
			if(Indices != dx || df != Object)
				return df[dx];
			else
				return this;
		}

		protected override Expression Derive(VisitCache cache, Expression s)
		{
			return (s == this) ? 1.0 : 0.0;
		}

		protected override Expression Simplify(VisitCache cache)
		{
			var df = cache[Object];
			Expression[] dx = Indices;
			for(int k = 0; k < Indices.Length; k++)
				if(Indices[k] != cache[Indices[k]])
					dx = new Expression[Indices.Length];
			if(dx != Indices)
				for(int k = 0; k < Indices.Length; k++)
					dx[k] = cache[Indices[k]];

			var oce = df as ObjectConstantExpression;
			if(null != oce)
			{
				FieldExpression fie;
				if(dx.Length == 1 && null != (fie = dx[0] as FieldExpression))
					return Expression.Constant(oce.Object.GetType().GetField(fie.FieldName).GetValue(oce.Object));
				else
				{
					throw new NotImplementedException();
					//var indexer = oce.Object.GetType().GetProperty("Item");
					//indexer.GetValue(oce.Object, (from i in 
				}
			}

			if(Indices == dx && df == Object)
				return this;
			else
				return df[dx];
		}

		public override string ToString()
		{
			if(Indices.Length == 1 && Indices[0] is FieldExpression)
				return Object.ToString() + "." + Indices[0].ToString();
			else
				return Object.ToString() + "[" + string.Join(", ", (from i in Indices
																					 select i.ToString())) + "]";
		}

		public override string ToCode()
		{
			if(Indices.Length == 1 && Indices[0] is FieldExpression)
				return Object.ToCode() + "." + Indices[0].ToCode();
			else
				return Object.ToCode() + "[" + string.Join(", ", (from i in Indices
																				  select "(int)" + i.ToCode())) + "]";
		}
	}
}
