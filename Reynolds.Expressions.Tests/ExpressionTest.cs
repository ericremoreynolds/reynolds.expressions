using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Reynolds.Expressions.Tests
{
	[TestFixture]
	public class ExpressionTest
	{
		[Test]
		public void Test()
		{
			var x = new Symbol("x");
			var y = new Symbol("y");

			Assert.AreSame(x + y, y + x);
			Assert.AreSame(x * y, y * x);

			Assert.AreSame(2.0 * x, x * 2.0);
		}

		[Test]
		public void SimplifyTest()
		{
			var x = new Symbol("x");
			var y = new Symbol("y");

			{
				var e1 = (x + y + y + x + y).Normalize();
				var e2 = (2 * x + 3 * y).Normalize();
				Assert.AreSame(e1, e2);
			}

			{
				var e1 = (x * x * y * y * y).Normalize();
				var e2 = (Expression.Pow[x, 2] * Expression.Pow[y, 3]).Normalize();
				Assert.AreSame(e1, e2);
			}
		}

		[Test]
		public void SubstituteTest()
		{
			var x = new Symbol("x");
			var y = new Symbol("y");

			var e1 = 2 * x + 3 * y;
			var e2 = e1.Substitute(x | 3, y | -2).Normalize();

			Assert.IsTrue(e2.IsZero);
		}

		[Test]
		public void ArrayTest()
		{
			var x = new Symbol("x");
			var y = new Symbol("y");

			int[] a = new int[] { 123, 111 };

			var e = 2 * x[y+1];

			Assert.IsTrue(e.Substitute(x | a, y | 0).NormalizesTo(222));
		}

		[Test]
		public void FunctionTest()
		{
			var x = new Symbol("x");
			var y = new Symbol("y");

			int[] a = new int[] { 123, 111 };

			var e1 = x[y + 1];

			Assert.IsTrue(e1.Substitute(x | Expression.Exp, y | 0).NormalizesTo(Math.E));
		}

		[Test]
		public void DeriveTest()
		{
			var x = new Symbol("x");
			var y = new Symbol("y");

			Assert.IsTrue(x.Derive(y).IsZero);
			Assert.IsFalse(x.Derive(x).IsZero);

			var e = 3 * Expression.Sin[2.0 * x] + Expression.Pow[y, 2] + x / y;

			Assert.IsTrue(e.Derive(x).NormalizesTo(6.0 * Expression.Cos[2.0 * x] + 1 / y));

			Assert.IsTrue(e.Derive(y).NormalizesTo(2 * y - x / Expression.Pow[y, 2]));
		}

		[Test]
		public void SumTest()
		{
			var x = new Symbol("x");
			var y = new Symbol("y");
			var e = -x + (3*x) + y;

			Assert.IsTrue(e.NormalizesTo(2 * x + y));
			Assert.IsTrue(e.Substitute(x | 1, y | 2).NormalizesTo(4));
			Assert.IsTrue(e.Derive(x).NormalizesTo(2));
		}

		[Test]
		public void ProductTest()
		{
			var x = new Symbol("x");
			var y = new Symbol("y");
			var e = Expression.Pow[x, 3] / x * y;

			Assert.IsTrue(e.NormalizesTo(Expression.Pow[x, 2] * y));
			Assert.IsTrue(e.Substitute(x | 3, y | 2).NormalizesTo(18));
			Assert.IsTrue(e.Derive(x).NormalizesTo(2 * x * y));
		}

		[Test]
		public void PowTest()
		{
			var x = new Symbol("x");
			var y = new Symbol("y");
			var e = Expression.Pow[x, y];

			Assert.IsTrue(Expression.Pow[Expression.Pow[x, 2], 3].NormalizesTo(Expression.Pow[x, 6]));
			Assert.IsTrue(e.Substitute(x | 2.0).Derive(y).NormalizesTo(Expression.Pow[2.0, y] * Math.Log(2.0)));
			Assert.IsTrue(e.Substitute(x | 2, y | 3).NormalizesTo(8));
		}

		[Test]
		public void SinTest()
		{
			var x = new Symbol("x");
			var e = Expression.Sin[x];
			Assert.IsTrue(e.Substitute(x | 2.0 * Math.PI).NormalizesTo(0));
			Assert.IsTrue(e.Derive(x).NormalizesTo(Expression.Cos[x]));
		}

		[Test]
		public void CosTest()
		{
			var x = new Symbol("x");
			var e = Expression.Cos[x];
			Assert.IsTrue(e.Substitute(x | 2.0 * Math.PI).NormalizesTo(1));
			Assert.IsTrue(e.Derive(x).NormalizesTo(-Expression.Sin[x]));
		}

		[Test]
		public void ExpTest()
		{
			var x = new Symbol("x");
			var e = Expression.Exp[2*x];
			Assert.IsTrue(e.Substitute(x | 0.5).NormalizesTo(Math.E));
			Assert.IsTrue(e.Derive(x).NormalizesTo(2 * e));
		}

		[Test]
		public void LogTest()
		{
			var x = new Symbol("x");
			var e = Expression.Log[x];
			Assert.IsTrue(e.Substitute(x | Math.E).NormalizesTo(1));
			Assert.IsTrue(e.Derive(x).NormalizesTo(1 / x));
		}

		class FieldTestClass
		{
			public int F = 2;
			public double G
			{
				get
				{
					return 3.0;
				}
			}
		}

		[Test]
		public void FieldTest()
		{
			var x = new Symbol("x");
			var y = new Symbol("y");

			var e = x[y];

			var c = new FieldTestClass();

			Assert.IsTrue(e.Substitute(x | c, y | Expression.Field("F")).NormalizesTo(c.F));
			Assert.IsTrue(e.Substitute(x | c, y | Expression.Field("G")).NormalizesTo(c.G));
		}
	}
}
