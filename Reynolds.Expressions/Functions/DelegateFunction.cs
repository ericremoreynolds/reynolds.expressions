using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions.Functions
{
	public class DelegateFunction : FunctionExpression
	{
		public delegate double EvaluateDelegate(double[] x);
		public delegate Expression PartialDerivativeDelegate(Expression[] x);
		public delegate Expression SimplifyDelegate(Expression[] x);
		public delegate string StringifyDelegate(Expression[] x);
		public delegate string CodifyDelegate(Expression[] x);

		protected EvaluateDelegate evaluate;
		protected PartialDerivativeDelegate[] partials;
		public SimplifyDelegate Simplify;
		public StringifyDelegate Stringify;
		public CodifyDelegate Codify;

		public void SetPartial(int i, PartialDerivativeDelegate partial)
		{
			this.partials[i] = partial;
		}

		public DelegateFunction(string name, string code, EvaluateDelegate evaluate, params PartialDerivativeDelegate[] partials)
		{
			this.Name = name;
			this.Code = code;
			this.evaluate = evaluate;
			//this.Simplify = Simplify;
			this.partials = partials == null ? new PartialDerivativeDelegate[1] : partials;
		}

		public override int Arity
		{
			get
			{
				return partials.Length;
			}
		}

		public string Name
		{
			get;
			protected set;
		}

		public string Code
		{
			get;
			protected set;
		}

		public override Expression GetPartialDerivative(int i, params Expression[] x)
		{
			return partials[i](x);
		}

		public override double Evaluate(params double[] x)
		{
			return evaluate(x);
		}

		public override Expression TrySimplify(params Expression[] x)
		{
			if(Simplify != null)
			{
				Expression e = Simplify(x);
				if(null != e)
					return e;
			}
			return base.TrySimplify(x);
		}

		public override string ToString(Expression[] x)
		{
			if(Stringify != null)
				return Stringify(x);
			else
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(Name).Append("(");
				for(int k = 0; k < x.Length; k++)
					sb.Append(k == 0 ? "" : ", ").Append(x[k].ToString());
				sb.Append(")");
				return sb.ToString();
			}
		}

		public override string ToCode(Expression[] x)
		{
			if(Codify != null)
				return Codify(x);
			else
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(Code).Append("(");
				for(int k = 0; k < x.Length; k++)
					sb.Append(k == 0 ? "" : ", ").Append(x[k].ToCode());
				sb.Append(")");
				return sb.ToString();
			}
		}
	}
}
