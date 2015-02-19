using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Expressions
{
	public abstract class Matrix
	{
		public abstract int Rows
		{
			get;
		}

		public abstract int Columns
		{
			get;
		}

		public abstract Matrix ZeroMatrix
		{
			get;
		}

		public abstract Matrix IdentityMatrix
		{
			get;
		}

		protected abstract Matrix Multiply(Matrix other);
		protected abstract Matrix Scale(double scalar);
		protected abstract Matrix Add(Matrix other);

		public static Matrix operator %(Matrix m, Matrix n)
		{
			return m.Multiply(n);
		}

		public static Matrix operator *(Matrix m, double s)
		{
			return m.Scale(s);
		}

		public static Matrix operator *(double s, Matrix m)
		{
			return m.Scale(s);
		}

		public static Matrix operator +(Matrix m, Matrix n)
		{
			return m.Add(n);
		}

		public abstract ArrayMatrix AsArrayMatrix
		{
			get;
		}
	}

	public class ArrayMatrix : Matrix
	{
		public ArrayMatrix(int rows, int columns)
		{
			this.Values = new double[rows, columns];
		}

		public ArrayMatrix(double[,] values)
		{
			this.Values = values;
		}

		public double[,] Values
		{
			get;
			protected set;
		}

		public double this[int i, int j]
		{
			get
			{
				return Values[i, j];
			}
			set
			{
				Values[i, j] = value;
			}
		}

		public override int Rows
		{
			get
			{
				return Values.GetLength(0);
			}
		}

		public override int Columns
		{
			get
			{
				return Values.GetLength(1);
			}
		}

		protected override Matrix Multiply(Matrix other)
		{
			throw new NotImplementedException();
		}

		protected override Matrix Scale(double scalar)
		{
			throw new NotImplementedException();
		}

		protected override Matrix Add(Matrix other)
		{
			throw new NotImplementedException();
		}

		public override ArrayMatrix AsArrayMatrix
		{
			get
			{
				return this;
			}
		}

		public override Matrix ZeroMatrix
		{
			get
			{
				return new ArrayMatrix(Rows, Columns);
			}
		}

		public override Matrix IdentityMatrix
		{
			get
			{
				if(Rows != Columns)
					throw new Exception("Can only get identity for square matrices.");
				double[,] v = new double[Rows, Columns];
				for(int k = 0; k < Rows; k++)
					v[k, k] = 1;
				return new ArrayMatrix(v);
			}
		}
	}
}
