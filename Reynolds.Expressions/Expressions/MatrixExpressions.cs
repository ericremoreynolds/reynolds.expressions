using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reynolds.Mappings;
using Reynolds.Expressions.Expressions;

namespace Reynolds.Expressions
{
	public class MatrixConstantExpression : Expression
	{
		public readonly Matrix Matrix;

		public override Expression Rows
		{
			get
			{
				return Matrix.Rows;
			}
		}

		public override Expression Columns
		{
			get
			{
				return Matrix.Columns;
			}
		}

		static WeakLazyMapping<Matrix, MatrixConstantExpression> instances = new WeakLazyMapping<Matrix, MatrixConstantExpression>(m => new MatrixConstantExpression(m));
		public static Expression Get(Matrix m)
		{
			return instances[m];
		}

		protected MatrixConstantExpression(Matrix m)
		{
			Matrix = m;
		}

		internal override Expression Derive(IDerivativeCache cache, Expression s)
		{
			return Get(Matrix.ZeroMatrix);
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return this;
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit(Matrix, typeof(Matrix));
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit("<").Emit(Rows).Emit(" x ").Emit(Columns).Emit(" matrix>");
		}
	}

	public delegate Expression MatrixGeneratorDelegate(Symbol i, Symbol j);

	public class MatrixExpression : Expression
	{
		public readonly Expression Element;

		public override bool IsMatrix
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
				return Element.IsZero;
			}
		}

		Expression rows;
		public override Expression Rows
		{
			get
			{
				return rows;
			}
		}

		Expression columns;
		public override Expression Columns
		{
			get
			{
				return columns;
			}
		}

		protected static Symbol RowIndex = new Symbol("i");
		protected static Symbol ColumnIndex = new Symbol("j");

		static WeakLazyMapping<Expression, Expression, Expression, MatrixExpression> instances = new WeakLazyMapping<Expression, Expression, Expression, MatrixExpression>((m, n, e) => new MatrixExpression(m, n, e));

		public static Expression Get(Expression rows, Expression cols, MatrixGeneratorDelegate element)
		{
			return instances[rows, cols, element(RowIndex, ColumnIndex)];
		}

		protected MatrixExpression(Expression rows, Expression cols, Expression element)
		{
			this.rows = rows;
			this.columns = cols;
			Element = element;
		}

		internal override Expression Derive(IDerivativeCache cache, Expression variable)
		{
			return Get(rows, columns, (i, j) => cache[Element, variable]);
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return Get(cache[rows], cache[columns], (i, j) => cache[Element]);
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			throw new NotImplementedException();
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit("(").Emit(Element).Emit(" : ").Emit(Rows).Emit(", ").Emit(Columns).Emit(")");
		}

		public override Expression Transpose()
		{
			return MatrixExpression.Get(Columns, Rows, (i, j) => Element[i | j, j | i]);
		}
	}

	public class MatrixMultiplyExpression : Expression
	{
		public readonly Expression[] Factors;

		public override Expression Rows
		{
			get
			{
				return Factors[0].Rows;
			}
		}

		public override Expression Columns
		{
			get
			{
				return Factors[Factors.Length - 1].Columns;
			}
		}

		public override bool IsMatrix
		{
			get
			{
				return true;
			}
		}

		protected static WeakLazyMapping<Expression[], MatrixMultiplyExpression> cache = new WeakLazyMapping<Expression[], MatrixMultiplyExpression>(es => new MatrixMultiplyExpression(es), null, ReferenceTypeArrayEqualityComparer<Expression>.Instance);
		public static Expression Get(params Expression[] factors)
		{
			List<Expression> fs = new List<Expression>();

			for(int k=0; k<factors.Length; k++)
			{
				var mme = factors[k] as MatrixMultiplyExpression;
				if(mme != null)
				{
					foreach(var f in mme.Factors)
						fs.Add(f);
				}
				else
					fs.Add(factors[k]);
			}

			var rows = fs[0].Rows;
			var cols = fs[fs.Count - 1].Columns;

			for(int k=0; k<fs.Count; k++)
				if(fs[k].IsZero)
					return MatrixExpression.Get(rows, cols, (i, j) => 0);

			for(int k=0; k<fs.Count-1; )
			{

				MatrixInverseExpression mie;
				if((null != (mie = fs[k] as MatrixInverseExpression) && mie.Matrix == fs[k + 1])
					|| (null != (mie = fs[k+1] as MatrixInverseExpression) && mie.Matrix == fs[k]))
				{
					fs.RemoveAt(k);
					fs.RemoveAt(k + 1);
					continue;
				}
				else if(fs[k].IsConstant && fs[k + 1].IsConstant)
				{
					fs[k] %= fs[k + 1].Value;
					fs.RemoveAt(k + 1);
					continue;
				}
				k++;
			}

			if(fs.Count == 0)
				return MatrixExpression.Get(rows, cols, (i, j) => Expression.Identity[i, j]);
			else if(fs.Count == 1)
				return fs[0];
			else
				return cache[fs.ToArray()];
		}

		protected MatrixMultiplyExpression(Expression[] factors)
		{
			this.Factors = factors;
		}

		internal override Expression Derive(IDerivativeCache cache, Expression variable)
		{
			List<Expression> terms = new List<Expression>();
			for(int k=0; k<Factors.Length; k++)
			{
				var dx = cache[Factors[k], variable];

				Expression[] factors = new Expression[Factors.Length];
				Array.Copy(Factors, factors, Factors.Length);
				factors[k] = dx;
				terms.Add(Get(factors));
			}
			return SumExpression.Get(terms.ToArray());
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return Get(Factors.Select(e => cache[e]).ToArray());
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit("(").Emit(Factors[0]).Emit(")");
			for(int k = 1; k < Factors.Length; k++)
				context.Emit(".Multiply(").Emit(Factors[k]).Emit(")");
		}

		public override void ToString(IStringifyContext context)
		{
			if(context.EnclosingOperator > StringifyOperator.Product)
				context.Emit("(");
			for(int k = 0; k < Factors.Length; k++)
			{
				if(k > 0)
					context.Emit(" % ");
				context.Emit(Factors[k], StringifyOperator.Product);
			}
			if(context.EnclosingOperator > StringifyOperator.Product)
				context.Emit(")");
		}
	}

	public class MatrixInverseExpression : Expression
	{
		public readonly Expression Matrix;

		public override Expression Rows
		{
			get
			{
				return Matrix.Rows;
			}
		}

		public override Expression Columns
		{
			get
			{
				return Matrix.Columns;
			}
		}

		public override bool IsMatrix
		{
			get
			{
				return true;
			}
		}

		protected static WeakLazyMapping<Expression, MatrixInverseExpression> cache = new WeakLazyMapping<Expression, MatrixInverseExpression>(m => new MatrixInverseExpression(m));
		public static Expression Get(Expression m)
		{
			if(m.IsConstant)
				return m.Value.Inverse();

			return cache[m];
		}

		protected MatrixInverseExpression(Expression m)
		{
			this.Matrix = m;
		}

		internal override Expression Derive(IDerivativeCache cache, Expression variable)
		{
			return Expression.MMult(this, cache[Matrix, variable], this);
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return Get(cache[Matrix]);
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit("(").Emit(Matrix).Emit(").Inverse()");
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit(Matrix, StringifyOperator.Exponent).Emit("^-1");
		}
	}

	public class MatrixTransposeExpression : Expression
	{
		public readonly Expression Matrix;

		public override Expression Rows
		{
			get
			{
				return Matrix.Columns;
			}
		}

		public override Expression Columns
		{
			get
			{
				return Matrix.Rows;
			}
		}

		public override bool IsMatrix
		{
			get
			{
				return true;
			}
		}

		protected static WeakLazyMapping<Expression, MatrixTransposeExpression> cache = new WeakLazyMapping<Expression, MatrixTransposeExpression>(m => new MatrixTransposeExpression(m));
		public static Expression Get(Expression m)
		{
			if(m.IsConstant)
				return m.Value.Transpose();

			return cache[m];
		}

		protected MatrixTransposeExpression(Expression m)
		{
			this.Matrix = m;
		}

		internal override Expression Derive(IDerivativeCache cache, Expression variable)
		{
			return cache[Matrix, variable].Transpose();
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return cache[Matrix].Transpose();
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit("(").Emit(Matrix).Emit(").Transpose()");
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit("").Emit(Matrix, StringifyOperator.Exponent).Emit("'");
		}
	}

	public class MatrixRowsExpression : Expression
	{
		public readonly Expression Matrix;

		protected static WeakLazyMapping<Expression, MatrixRowsExpression> cache = new WeakLazyMapping<Expression, MatrixRowsExpression>(m => new MatrixRowsExpression(m));
		public static Expression Get(Expression m)
		{
			return cache[m];
		}

		public MatrixRowsExpression(Expression matrix)
		{
			Matrix = matrix;
		}

		internal override Expression Derive(IDerivativeCache cache, Expression s)
		{
			throw new NotImplementedException();
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return cache[Matrix].Rows;
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit(Matrix).Emit(".Rows");
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit(Matrix).Emit(".Rows");
		}
	}

	public class MatrixColumnsExpression : Expression
	{
		public readonly Expression Matrix;

		protected static WeakLazyMapping<Expression, MatrixColumnsExpression> cache = new WeakLazyMapping<Expression, MatrixColumnsExpression>(m => new MatrixColumnsExpression(m));
		public static Expression Get(Expression m)
		{
			return cache[m];
		}

		public MatrixColumnsExpression(Expression matrix)
		{
			Matrix = matrix;
		}

		internal override Expression Derive(IDerivativeCache cache, Expression s)
		{
			throw new NotImplementedException();
		}

		protected override Expression Substitute(VisitCache cache)
		{
			return cache[Matrix].Rows;
		}

		public override void GenerateCode(ICodeGenerationContext context)
		{
			context.Emit(Matrix).Emit(".Columns");
		}

		public override void ToString(IStringifyContext context)
		{
			context.Emit(Matrix).Emit(".Columns");
		}
	}
}
