//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Reynolds.Mappings;

//namespace Reynolds.Expressions.Expressions
//{
//   public class CastExpression : Expression
//   {
//      //public readonly Type Type;
//      public readonly Expression Inner;

//      static WeakLazyMapping<Type, Expression, CastExpression> cache = new WeakLazyMapping<Type, Expression, CastExpression>((t, e) => new CastExpression(t, e));
//      public static Expression Get(Type type, Expression inner)
//      {
//         return cache[type, inner];
//      }

//      protected CastExpression(Type type, Expression inner)
//      {
//         this.Type = type;
//         this.Inner = inner;
//      }

//      protected override Expression Derive(VisitCache cache, Expression s)
//      {
//         throw new NotImplementedException();
//      }

//      protected override Expression Substitute(VisitCache cache)
//      {
//         return Get(Type, cache[Inner]);
//      }

//      public override void ToString(IStringifyContext context)
//      {
//         context.Emit("(").Emit(Type.FullName).Emit(")").Emit(Inner);
//      }

//      public override void GenerateCode(ICodeGenerationContext context, Expression[] arguments)
//      {
//         context.Emit("(").Emit(Type.FullName).Emit(")").Emit(Inner);
//      }
//   }
//}
