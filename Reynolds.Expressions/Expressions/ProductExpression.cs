//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Reynolds.Mappings;

//namespace Reynolds.Expressions.Expressions
//{
//   public class ProductExpression : Expression
//   {
//      public readonly Expression[] Factors;

//      static WeakLazyMapping<Expression[], ProductExpression> productExpressions = new WeakLazyMapping<Expression[], ProductExpression>(es => new ProductExpression(es), null, ReferenceTypeArrayEqualityComparer<Expression>.Instance);
//      static internal Expression Get(params Expression[] factors)
//      {
//         Dictionary<Expression, Expression> newFactors = new Dictionary<Expression, Expression>();
//         List<Expression> nonCommutativeFactors = new List<Expression>();
//         dynamic coefficient = 1;

//         Action<Expression> addFactor = delegate(Expression e)
//         {
//            if(e.IsScalar)
//            {
//               if(e.IsConstant && e.IsScalar)
//                  coefficient *= e.Value;
//               else
//               {
//                  ApplicationExpression ae = e as ApplicationExpression;
//                  if(null != ae && ae.Applicand == Expression.Pow)
//                  {
//                     Expression p;
//                     if(!newFactors.TryGetValue(ae.Arguments[0], out p))
//                        p = 0;
//                     p += ae.Arguments[1];
//                     newFactors[ae.Arguments[0]] = p;
//                  }
//                  else
//                  {
//                     Expression p;
//                     if(!newFactors.TryGetValue(e, out p))
//                        p = 0;
//                     p += 1;
//                     newFactors[e] = p;
//                  }
//               }
//            }
//            else
//               nonCommutativeFactors.Add(e);
//         };

//         foreach(var t in factors)
//         {
//            var sf = t;
//            var pe = sf as ProductExpression;
//            if(pe != null)
//            {
//               foreach(var pef in pe.Factors)
//                  addFactor(pef);
//            }
//            else
//               addFactor(sf);
//         }

//         List<Expression> fs = new List<Expression>();
//         foreach(var kv in newFactors)
//         {
//            var p = kv.Value;
//            if(p.IsConstant)
//            {
//               if(kv.Key.IsConstant)
//                  coefficient *= Math.Pow(kv.Key.Value, p.Value);
//               else if(p.Value == 1)
//                  fs.Add(kv.Key);
//               else if(p.Value != 0)
//                  fs.Add(Expression.Pow[kv.Key, p]);
//            }
//            else
//               fs.Add(Expression.Pow[kv.Key, p]);
//         }

//         fs.Sort();

//         if(coefficient != 1)
//            fs.Insert(0, coefficient);

//         fs.AddRange(nonCommutativeFactors);
			
//         factors = fs.ToArray();

//         if(factors.Length == 0)
//            return 1;

//         if(factors.Length == 1)
//            return factors[0];

//         return productExpressions[factors];
//      }

//      bool isScalar;
//      public override bool IsScalar
//      {
//         get
//         {
//            return isScalar;
//         }
//      }

//      protected ProductExpression(Expression[] factors)
//      {
//         this.Factors = factors;
//         isScalar = factors.All(e => e.IsScalar);
//      }

//      protected override Expression Substitute(VisitCache cache)
//      {
//         bool changed = false;
//         Expression[] newFactors = new Expression[Factors.Length];
//         for(int k = 0; k < Factors.Length; k++)
//         {
//            newFactors[k] = cache[Factors[k]];
//            changed = changed || newFactors[k] != Factors[k];
//         }
//         if(changed)
//            return Get(newFactors);
//         return this;
//      }

//      protected override Expression Derive(VisitCache cache, Expression s)
//      {
//         List<Expression> terms = new List<Expression>();
//         foreach(var f in Factors)
//         {
//            var df = cache[f];
//            if(!df.IsZero)
//               terms.Add(df * this / f);
//         }
//         return SumExpression.Get(terms.ToArray());
//      }

//      public override void ToString(IStringifyContext context)
//      {
//         if(context.EnclosingOperator > StringifyOperator.Product)
//            context.Emit("(");
//         bool first = true;
//         for(int k = 0; k < Factors.Length; k++)
//         {
//            if(!first)
//               context.Emit("*");
//            context.Emit(Factors[k], StringifyOperator.Product);
//            first = false;
//         }
//         if(context.EnclosingOperator > StringifyOperator.Product)
//            context.Emit(")");
//      }

//      public override void GenerateCode(ICodeGenerationContext context)
//      {
//         context.Emit("(");
//         bool first = true;
//         for(int k = 0; k < Factors.Length; k++)
//         {
//            if(!first)
//               context.Emit("*");
//            context.Emit(Factors[k]);
//            first = false;
//         }
//         context.Emit(")");
//      }
//   }
//}
