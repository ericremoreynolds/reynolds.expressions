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
			var x = Expression.Symbol<double>("x");
			var y = Expression.Symbol<double>("y");

			Assert.That(x + y == y + x);
			Assert.That(x * y == y * x);

			Assert.That(2.0 * x == x * 2.0);

			Assert.That(x * (y * y) == (x * y) * y);
		}

		[Test]
		public void SimplifyTest()
		{
			var x = Expression.Symbol<double>("x");
			var y = Expression.Symbol<double>("y");

			{
				var e1 = x + y + y + x + y;
				var e2 = 2 * x + 3 * y;
				Assert.AreSame(e1, e2);
			}

			{
				var e1 = x * x * y * y * y;
				var e2 = Expression.Pow[x, 2] * Expression.Pow[y, 3];
				Assert.AreSame(e1, e2);
			}
		}

		[Test]
		public void SubstituteTest()
		{
			var x = Expression.Symbol<double>("x");
			var y = Expression.Symbol<double>("y");

			var e1 = 2 * x + 3 * y;
			var e2 = e1[x | 3, y | -2];

			Assert.IsTrue(e2.IsZero);

			var cf1 = e1.Compile<Func<double, double, double>>(x, y);
			Assert.IsTrue(e1[x | 3, y | 5].Value == cf1(3, 5));
		}

		[Test]
		public void ArrayTest()
		{
			var x = Expression.Symbol<double>("x");
			var y = Expression.Symbol<double>("y");

			int[] a = new int[] { 123, 111 };

			var e = 2 * x[y+1];

			Assert.IsTrue(e[x | Expression.Constant(a), y | 0].Value == 222);

			var cf = e.Compile<Func<int[], int, int>>(x, y);
			Assert.AreEqual(246, cf(a, -1));

			var cf2 = e[x | Expression.Constant(a)].Compile<Func<int, int>>(y);
			Assert.AreEqual(246, cf2(-1));
			Assert.AreEqual(222, cf2(0));
		}

		[Test]
		public void FunctionTest()
		{
			var x = Expression.Symbol<double>("x");
			var y = Expression.Symbol<double>("y");

			var e1 = x[y + 1];

			Assert.That(e1[x | Expression.Exp, y | 0].Value, Is.InRange(Math.E - 0.001, Math.E + 0.001));

			//var cf2 = e1.Compile<Func<Func<double, double>, double, double>>(x, y);
			//Assert.That(cf2(Math.Exp, 0.0), Is.InRange(Math.E - 0.001, Math.E + 0.001));
		}

		[Test]
		public void DeriveTest()
		{
			var x = Expression.Symbol<double>("x");
			var y = Expression.Symbol<double>("y");

			Assert.IsTrue(x.Derive(y).IsZero);
			Assert.IsFalse(x.Derive(x).IsZero);

			var e = 3 * Expression.Sin[2.0 * x] + Expression.Pow[y, 2] + x / y;

			Assert.IsTrue(e.Derive(x) == 6.0 * Expression.Cos[2.0 * x] + 1 / y);

			Assert.IsTrue(e.Derive(y) == 2 * y - x / Expression.Pow[y, 2]);
		}

		[Test]
		public void SumTest()
		{
			var x = Expression.Symbol<double>("x");
			var y = Expression.Symbol<double>("y");
			var e = -x + (3*x) + y;

			Assert.IsTrue(e == 2 * x + y);
			Assert.IsTrue(e[x | 1, y | 2].Value == 4);
			Assert.IsTrue(e.Derive(x).Value == 2);

			var cf = e.Compile<Func<double, double, double>>(x, y);
			Assert.IsTrue(e[x | 3.0, y | 2.0].Value == cf(3.0, 2.0));
		}

		[Test]
		public void ProductTest()
		{
			var x = Expression.Symbol<double>("x");
			var y = Expression.Symbol<double>("y");
			var e = Expression.Pow[x, 3] / x * y;

			Assert.IsTrue(e == Expression.Pow[x, 2] * y);
			Assert.IsTrue(e[x | 3, y | 2].Value == 18);
			Assert.IsTrue(e.Derive(x) == 2 * x * y);

			var cf = e.Compile<Func<double, double, double>>(x, y);
			Assert.IsTrue(e[x | 3.0, y | 2.0].Value == cf(3.0, 2.0));
		}

		[Test]
		public void PowTest()
		{
			var x = Expression.Symbol<double>("x");
			var y = Expression.Symbol<double>("y");
			var e = Expression.Pow[x, y];

			Assert.IsTrue(Expression.Pow[Expression.Pow[x, 2], 3] == Expression.Pow[x, 6]);
			Assert.IsTrue(e[x | 2.0].Derive(y) == Expression.Pow[2.0, y] * Math.Log(2.0));
			Assert.IsTrue(e[x | 2, y | 3].Value == 8);

			var cf = e.Compile<Func<double, double, double>>(x, y);
			Assert.IsTrue(e[x | 3.0, y | 2.0].Value == cf(3.0, 2.0));
		}

		[Test]
		public void SinTest()
		{
			var x = Expression.Symbol<double>("x");
			var e = Expression.Sin[x];
			Assert.That(e[x | 2.0 * Math.PI].Value, Is.InRange(-0.0001, 0.0001));
			Assert.IsTrue(e.Derive(x) == Expression.Cos[x]);

			var cf = e.Compile<Func<double, double>>(x);
			Assert.IsTrue(e[x | 3.0].Value == cf(3.0));
		}

		[Test]
		public void CosTest()
		{
			var x = Expression.Symbol<double>("x");
			var e = Expression.Cos[x];
			Assert.That(e[x | 2.0 * Math.PI].Value, Is.InRange(0.9999, 1.0001));
			Assert.IsTrue(e.Derive(x) == -Expression.Sin[x]);

			var cf = e.Compile<Func<double, double>>(x);
			Assert.IsTrue(e[x | 3.0].Value == cf(3.0));
		}

		[Test]
		public void ExpTest()
		{
			var x = Expression.Symbol<double>("x");
			var e = Expression.Exp[2*x];
			Assert.IsTrue(e[x | 0.5].Value == Math.E);
			Assert.IsTrue(e.Derive(x) == 2 * e);

			var cf = e.Compile<Func<double, double>>(x);
			Assert.IsTrue(e[x | 3.0].Value == cf(3.0));
		}

		[Test]
		public void LogTest()
		{
			var x = Expression.Symbol<double>("x");
			var e = Expression.Log[x];
			Assert.IsTrue(e[x | Math.E].Value == 1);
			Assert.IsTrue(e.Derive(x) == 1 / x);

			var cf = e.Compile<Func<double, double>>(x);
			Assert.IsTrue(e[x | 3.0].Value == cf(3.0));
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
			var x = Expression.Symbol<double>("x");
			var y = Expression.Symbol<double>("y");

			var e = Expression.Exp[2*x[y]];

			var c = new FieldTestClass();

			Assert.IsTrue(e[x | Expression.Constant(c), y | Expression.Field("F")].Value == Math.Exp(2 * c.F));
			Assert.IsTrue(e[x | Expression.Constant(c), y | Expression.Field("G")].Value == Math.Exp(2 * c.G));

			var e2 = e[y | Expression.Field("F")];
			var cf2 = e2.Compile<Func<FieldTestClass, double>>(x);
			Assert.That(cf2(c), Is.EqualTo(Math.Exp(4)));

			var e3 = e[y | Expression.Field("G")];
			var cf3 = e3.Compile<Func<FieldTestClass, double>>(x);
			Assert.That(cf3(c), Is.EqualTo(Math.Exp(6.0)));

			var e4 = e.Derive(x[y]);
			Assert.That(e4 == 2 * Expression.Exp[2 * x[y]]);
		}

		[Test]
		public void CompileTest()
		{
			var x = Expression.Symbol<double>("x");
			var y = Expression.Symbol<double>("y");

			var e = Expression.Cos[2.0 * x] + (y + 1) * (3 + Expression.Cos[2.0 * x]);
			var cf = e.Compile<Func<double, double, double>>(x, y);

			Assert.That(e[x | 2.0, y | 3.0].Value == cf(2.0, 3.0));
		}
	}
}
