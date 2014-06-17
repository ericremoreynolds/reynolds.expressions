//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Reynolds.Mappings;

//namespace Reynolds.Expressions.Functions.Matrix
//{
//   public class MatrixInverseExpression : Expression
//   {
//      public readonly Expression M;

//      protected static WeakLazyMapping<Expression, MatrixInverseExpression> cache = new WeakLazyMapping<Expression, MatrixInverseExpression>(m => new MatrixInverseExpression(m));
//      public static Expression Get(Expression m)
//      {
//         return cache[m];
//      }

//      protected MatrixInverseExpression(Expression m)
//      {
//         this.M = m;
//      }

//      protected override Expression Derive(VisitCache cache, Expression s)
//      {
//         return this[M.Derive(s)[this]];
//      }

//      //protected override Expression Normalize(INormalizeContext context)
//      //{
//      //   return this;
//      //}

//      protected override Expression Normalize(INormalizeContext context, Expression[] arguments)
//      {
//         return this[M[arguments]];
//      }

//      protected override Expression Substitute(VisitCache cache)
//      {
//         throw new NotImplementedException();
//      }

//      public override void GenerateCode(ICodeGenerationContext context)
//      {
//         throw new NotImplementedException();
//      }

//      public override void ToString(IStringifyContext context)
//      {
//         throw new NotImplementedException();
//      }
//   }
//}
