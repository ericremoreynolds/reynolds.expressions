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
			var e2 = e1[x | 3, y | -2].Normalize();

			Assert.IsTrue(e2.IsZero);

			var cf1 = e1.Compile(x, y);
			Assert.IsTrue(e1[x | 3, y | 5].NormalizesTo(cf1(3, 5)));
		}

		[Test]
		public void ArrayTest()
		{
			var x = new Symbol("x");
			var y = new Symbol("y");

			int[] a = new int[] { 123, 111 };

			var e = 2 * x[y+1];

			Assert.IsTrue(e[x | a, y | 0].NormalizesTo(222));

			var cf = e.Compile<Func<int[], int, int>>(x, y);
			Assert.AreEqual(246, cf(a, -1));
		}

		[Test]
		public void FunctionTest()
		{
			var x = new Symbol("x");
			var y = new Symbol("y");

			var e1 = x[y + 1];

			Assert.That(e1[x | Expression.Exp, y | 0].Normalize().Value, Is.InRange(Math.E - 0.001, Math.E + 0.001));

			//var cf2 = e1.Compile<Func<Func<double, double>, double, double>>(x, y);
			//Assert.That(cf2(Math.Exp, 0.0), Is.InRange(Math.E - 0.001, Math.E + 0.001));
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
			Assert.IsTrue(e[x | 1, y | 2).NormalizesTo(4));
			Assert.IsTrue(e.Derive(x).NormalizesTo(2));

			var cf = e.Compile(x, y);
			Assert.IsTrue(e[x | 3.0, y | 2.0).NormalizesTo(cf(3.0, 2.0)));
		}

		[Test]
		public void ProductTest()
		{
			var x = new Symbol("x");
			var y = new Symbol("y");
			var e = Expression.Pow[x, 3] / x * y;

			Assert.IsTrue(e.NormalizesTo(Expression.Pow[x, 2] * y));
			Assert.IsTrue(e[x | 3, y | 2).NormalizesTo(18));
			Assert.IsTrue(e.Derive(x).NormalizesTo(2 * x * y));

			var cf = e.Compile(x, y);
			Assert.IsTrue(e[x | 3.0, y | 2.0).NormalizesTo(cf(3.0, 2.0)));
		}

		[Test]
		public void PowTest()
		{
			var x = new Symbol("x");
			var y = new Symbol("y");
			var e = Expression.Pow[x, y];

			Assert.IsTrue(Expression.Pow[Expression.Pow[x, 2], 3].NormalizesTo(Expression.Pow[x, 6]));
			Assert.IsTrue(e[x | 2.0).Derive(y).NormalizesTo(Expression.Pow[2.0, y] * Math.Log(2.0)));
			Assert.IsTrue(e[x | 2, y | 3).NormalizesTo(8));

			var cf = e.Compile(x, y);
			Assert.IsTrue(e[x | 3.0, y | 2.0).NormalizesTo(cf(3.0, 2.0)));
		}

		[Test]
		public void SinTest()
		{
			var x = new Symbol("x");
			var e = Expression.Sin[x];
			Assert.That(e[x | 2.0 * Math.PI).Normalize().Value, Is.InRange(-0.0001, 0.0001));
			Assert.IsTrue(e.Derive(x).NormalizesTo(Expression.Cos[x]));

			var cf = e.Compile(x);
			Assert.IsTrue(e[x | 3.0).NormalizesTo(cf(3.0)));
		}

		[Test]
		public void CosTest()
		{
			var x = new Symbol("x");
			var e = Expression.Cos[x];
			Assert.That(e[x | 2.0 * Math.PI).Normalize().Value, Is.InRange(0.9999, 1.0001));
			Assert.IsTrue(e.Derive(x).NormalizesTo(-Expression.Sin[x]));

			var cf = e.Compile(x);
			Assert.IsTrue(e[x | 3.0).NormalizesTo(cf(3.0)));
		}

		[Test]
		public void ExpTest()
		{
			var x = new Symbol("x");
			var e = Expression.Exp[2*x];
			Assert.IsTrue(e[x | 0.5).NormalizesTo(Math.E));
			Assert.IsTrue(e.Derive(x).NormalizesTo(2 * e));

			var cf = e.Compile(x);
			Assert.IsTrue(e[x | 3.0).NormalizesTo(cf(3.0)));
		}

		[Test]
		public void LogTest()
		{
			var x = new Symbol("x");
			var e = Expression.Log[x];
			Assert.IsTrue(e[x | Math.E).NormalizesTo(1));
			Assert.IsTrue(e.Derive(x).NormalizesTo(1 / x));

			var cf = e.Compile(x);
			Assert.IsTrue(e[x | 3.0).NormalizesTo(cf(3.0)));
		}

		public class FieldTestClass
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

			Assert.IsTrue(e[x | c, y | Expression.Field("F")).NormalizesTo(c.F));
			Assert.IsTrue(e[x | c, y | Expression.Field("G")).NormalizesTo(c.G));

			var e2 = e[y | Expression.Field("F"));
			var cf2 = e2.Compile<Func<FieldTestClass, int>>(x);
			Assert.AreEqual(2, cf2(c));

			var e3 = e[y | Expression.Field("G"));
			var cf3 = e3.Compile<Func<FieldTestClass, double>>(x);
			Assert.AreEqual(3.0, cf3(c));
		}
	}
}
