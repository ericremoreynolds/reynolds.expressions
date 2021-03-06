﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CSharp;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using Reynolds.Mappings;

namespace Reynolds.Expressions
{
	public interface ICodeGenerationContext
	{
		ICodeGenerationContext Emit(Expression e);
		ICodeGenerationContext Emit(Type t);
		ICodeGenerationContext Emit(double d);
		ICodeGenerationContext Emit(int i);
		ICodeGenerationContext Emit(string s);
		ICodeGenerationContext Emit(object o, Type type);
		ICodeGenerationContext Emit(Expression e, Expression[] args);
	}

	public abstract class ExpressionCompilerArgument
	{
		public static implicit operator ExpressionCompilerArgument(Symbol symbol)
		{
			return new ExpressionCompilerSymbolArgumentInfo(symbol);
		}
	}

	internal class ExpressionCompilerSymbolArgumentInfo : ExpressionCompilerArgument
	{
		public readonly Symbol Symbol;

		public ExpressionCompilerSymbolArgumentInfo(Symbol symbol)
		{
			this.Symbol = symbol;
		}
	}

	internal class ExpressionCompilerExpressionArgumentInfo : ExpressionCompilerArgument
	{
		public readonly Expression Expression;
		public readonly bool IsRef;

		public ExpressionCompilerExpressionArgumentInfo(Expression expression, bool isRef)
		{
			this.Expression = expression;
			this.IsRef = isRef;
		}
	}

	internal class ExpressionCompilerExpressionArrayArgumentInfo : ExpressionCompilerArgument
	{
		public readonly Expression[] Expressions;
		public readonly bool CreateIfNull;

		public ExpressionCompilerExpressionArrayArgumentInfo(Expression[] expressions, bool createIfNull)
		{
			this.Expressions = expressions;
			this.CreateIfNull = createIfNull;
		}
	}

	public class ExpressionCompiler
	{
		class CodeGenerationContext : ICodeGenerationContext, IDisposable
		{
			public class ObjectValueInfo
			{
				public string Label;
			}

			CSharpCodeProvider provider = new CSharpCodeProvider();
			StringBuilder sb = new StringBuilder();
			public readonly DictionaryMapping<object, Type, ObjectValueInfo> ObjectValues = new DictionaryMapping<object, Type, ObjectValueInfo>();
			public readonly HashSet<Assembly> Assemblies = new HashSet<Assembly>();

			public ICodeGenerationContext Emit(Type type)
			{
				Assemblies.Add(type.Assembly);
				if(type.IsByRef)
					type = type.GetElementType();
				sb.Append(provider.GetTypeOutput(new CodeTypeReference(type)));
				return this;
			}

			public readonly Dictionary<Expression, string> CachedExpressions = new Dictionary<Expression, string>();

			public ICodeGenerationContext Emit(object obj, Type type)
			{
				ObjectValueInfo info;
				if(!ObjectValues.TryGetValue(obj, type, out info))
					ObjectValues[obj, type] = info = new ObjectValueInfo()
					{
						Label = "v" + ObjectValues.Count.ToString()
					};
				sb.Append(info.Label);
				return this;
			}

			public ICodeGenerationContext Emit(Expression e, Expression[] args)
			{
				e.GenerateCode(this, args);
				return this;
			}

			public override string ToString()
			{
				return sb.ToString();
			}

			void IDisposable.Dispose()
			{
				provider.Dispose();
			}

			public ICodeGenerationContext Emit(Expression e)
			{
				string label;
				if(this.CachedExpressions.TryGetValue(e, out label))
					sb.Append(label);
				else
					e.GenerateCode(this);
				return this;
			}

			public ICodeGenerationContext Emit(double d)
			{
				sb.Append(d);
				return this;
			}

			public ICodeGenerationContext Emit(int i)
			{
				sb.Append(i);
				return this;
			}

			public ICodeGenerationContext Emit(string s)
			{
				sb.Append(s);
				return this;
			}
		}

		class ExpressionInstanceCounter : ICodeGenerationContext
		{
			public class ExpressionCounter
			{
				public int Index;
				public int Count;
				public int Priority;
				public Expression Expression;
			}

			Dictionary<Expression, ExpressionCounter> counters = new Dictionary<Expression, ExpressionCounter>();

			public List<ExpressionCounter> GetInfos()
			{
				var list = new List<ExpressionCounter>();
				foreach(var ec in counters.Values)
					if(ec.Count > 1 && !ec.Expression.IsConstant)
						list.Add(ec);

				list.Sort((a, b) => -a.Priority.CompareTo(b.Priority));

				return list;
			}

			int currentPriority = 0;
			int currentIndex = 0;
			int countInc = 1;

			public ExpressionInstanceCounter()
			{
			}

			public ICodeGenerationContext Emit(Expression e)
			{
				ExpressionCounter ec;
				var oldCountInc = countInc;
				countInc = 0;
				if(!counters.TryGetValue(e, out ec))
				{
					counters[e] = ec = new ExpressionCounter()
					{
						Index = currentIndex++,
						Expression = e
					};
					countInc = 1;
				}
				ec.Count += oldCountInc;
				ec.Priority = Math.Max(currentPriority, ec.Priority);
				var oldPriority = currentPriority;
				currentPriority = ec.Priority + 1;
				e.GenerateCode(this);
				currentPriority = oldPriority;
				countInc = oldCountInc;
				return this;
			}

			public ICodeGenerationContext Emit(Type t)
			{
				return this;
			}

			public ICodeGenerationContext Emit(double d)
			{
				return this;
			}

			public ICodeGenerationContext Emit(int i)
			{
				return this;
			}

			public ICodeGenerationContext Emit(string s)
			{
				return this;
			}

			public ICodeGenerationContext Emit(object o, Type type)
			{
				return this;
			}

			public ICodeGenerationContext Emit(Expression e, Expression[] args)
			{
				e.GenerateCode(this, args);
				return this;
			}
		}

		class CompilationInfo
		{
			public Expression ReturnExpression;
			public Type DelegateType;
			public ExpressionCompilerArgument[] Arguments;
			public Action<Delegate> Callback;
		}

		List<CompilationInfo> jobs = new List<CompilationInfo>();

		abstract class Job
		{
			public abstract void GenerateCode(ICodeGenerationContext context);
			public abstract void Emit(ICodeGenerationContext context);
		}

		class ExpressionArrayJob : Job
		{
			public ExpressionArrayJob(string name, Expression[] expressions, ExpressionCompilerExpressionArrayArgumentInfo argument)
			{
				this.Name = name;
				this.Argument = argument;
				this.Expressions = expressions;
			}

			public readonly Expression[] Expressions;
			public readonly ExpressionCompilerExpressionArrayArgumentInfo Argument;
			public readonly string Name;

			public override void GenerateCode(ICodeGenerationContext context)
			{
				foreach(var e in Expressions)
					e.GenerateCode(context);
			}

			public override void Emit(ICodeGenerationContext context)
			{
				if(Argument.CreateIfNull)
					context.Emit("if(" + Name + " != null) {\n");
				else
					context.Emit("{ " + Name + " = new double[" + Expressions.Length.ToString() + "];\n");
				for(int k = 0; k < Expressions.Length; k++)
					context.Emit(Name + "[" + k.ToString() + "] = ").Emit(Expressions[k]).Emit(";\n");
				context.Emit("}");
			}
		}

		class OutExpressionJob : Job
		{
			public OutExpressionJob(string name, Expression expression)
			{
				this.Name = name;
				this.Expression = expression;
			}

			public readonly Expression Expression;
			public readonly string Name;

			public override void GenerateCode(ICodeGenerationContext context)
			{
				Expression.GenerateCode(context);
			}

			public override void Emit(ICodeGenerationContext context)
			{
				context.Emit(Name + " = ").Emit(Expression).Emit(";\n");
			}
		}

		class ReturnJob : Job
		{
			public readonly Expression Expression;

			public ReturnJob(Expression expression)
			{
				this.Expression = expression;
			}

			public override void GenerateCode(ICodeGenerationContext context)
			{
				Expression.GenerateCode(context);
			}

			public override void Emit(ICodeGenerationContext context)
			{
				context.Emit("return ").Emit(Expression).Emit(";\n");
			}
		}

		public static ExpressionCompilerArgument OutputArgument(Expression expression)
		{
			return new ExpressionCompilerExpressionArgumentInfo(expression, false);
		}

		public static ExpressionCompilerArgument OutputArgument(Expression[] expressions)
		{
			return new ExpressionCompilerExpressionArrayArgumentInfo(expressions, true);
		}

		public Delegate[] CompileAll()
		{
			using(var context = new CodeGenerationContext())
			{
				context.Emit("using System;\n");
				context.Emit("public static class GeneratedFunction {\n");

				for(int k = 0; k < jobs.Count; k++)
				{
					var mi = jobs[k].DelegateType.GetMethod("Invoke");
					var args = mi.GetParameters();

					List<Job> ejs = new List<Job>();

					ExpressionSubstitution[] subs;
					{
						//ExpressionSubstitution[] tmpSubs = new ExpressionSubstitution[jobs[k].Arguments.Length];
						List<ExpressionSubstitution> tmpSubs = new List<ExpressionSubstitution>();
						for(int j = 0; j < jobs[k].Arguments.Length; j++)
						{
							//subs[j] = jobs[k].Arguments[j] | new Symbol("x" + j.ToString());
							ExpressionCompilerSymbolArgumentInfo symbol =jobs[k].Arguments[j] as ExpressionCompilerSymbolArgumentInfo;
							if(symbol != null)
								tmpSubs.Add(symbol.Symbol | new Symbol("x" + j.ToString()));
						}
						subs = tmpSubs.ToArray();
					}


					for(int j = 0; j < jobs[k].Arguments.Length; j++)
					{
						ExpressionCompilerExpressionArgumentInfo exarg = jobs[k].Arguments[j] as ExpressionCompilerExpressionArgumentInfo;
						if(exarg != null)
						{
							ejs.Add(new OutExpressionJob("x" + j.ToString(), exarg.Expression.Substitute(subs)));
							continue;
						}

						ExpressionCompilerExpressionArrayArgumentInfo exaarg = jobs[k].Arguments[j] as ExpressionCompilerExpressionArrayArgumentInfo;
						if(exaarg != null)
						{
							ejs.Add(new ExpressionArrayJob("x" + j.ToString(), (from ee in exaarg.Expressions select ee.Substitute(subs)).ToArray(), exaarg));
						}
					}
					ejs.Add(new ReturnJob(jobs[k].ReturnExpression.Substitute(subs)));

					var e = jobs[k].ReturnExpression.Substitute(subs);

					context.Emit("public static ").Emit(mi.ReturnType).Emit(" f").Emit(k).Emit("(");

					for(int j = 0; j < args.Length; j++)
					{
						if(j > 0)
							context.Emit(", ");
						if(args[j].IsOut)
							context.Emit(args[j].IsIn ? "ref " : "out ");
						//string typeName = args[j].ParameterType;
						//if(typeName.EndsWith("&"))
						//   typeName = typeName.Substring(0, typeName.Length - 1);
						context.Emit(args[j].ParameterType).Emit(" x").Emit(j);
					}
					context.Emit(")\n{\n ");

					ExpressionInstanceCounter counters = new ExpressionInstanceCounter();

					foreach(var ej in ejs)
						ej.GenerateCode(counters);

					e.GenerateCode(counters);
					foreach(var ec in counters.GetInfos())
					{
						string label = "z" + ec.Index.ToString();
						context.Emit("var ").Emit(label).Emit(" = ").Emit(ec.Expression).Emit(";\n");
						context.CachedExpressions.Add(ec.Expression, label);
					}

					foreach(var ej in ejs)
						ej.Emit(context);

					//context.Emit("return ").Emit(e).Emit(";\n }\n");
					context.Emit("}\n");
				}

				foreach(var kv in context.ObjectValues)
					context.Emit("public static ").Emit(kv.Key2).Emit(" ").Emit(kv.Value.Label).Emit(";\n");

				context.Emit("}");

				string code = context.ToString();

				CompilerParameters parameters = new CompilerParameters();
				parameters.GenerateInMemory = true;
				parameters.TreatWarningsAsErrors = false;
				parameters.GenerateExecutable = false;
				parameters.CompilerOptions = "/optimize";
				parameters.IncludeDebugInformation = false;
				foreach(var assembly in context.Assemblies)
					parameters.ReferencedAssemblies.Add(assembly.Location);

				CompilerResults results = new CSharpCodeProvider().CompileAssemblyFromSource(parameters, new string[] { code });

				if(results.Errors.HasErrors)
					throw new Exception("Compile error: " + results.Errors[0].ToString()); //, results.Errors);

				Delegate[] result = new Delegate[jobs.Count];
				Type generatedClassType = results.CompiledAssembly.GetModules()[0].GetType("GeneratedFunction");
				for(int k = 0; k < jobs.Count; k++)
				{
					var mi = generatedClassType.GetMethod("f" + k.ToString());
					result[k] = Delegate.CreateDelegate(jobs[k].DelegateType, mi);
					if(jobs[k].Callback != null)
						jobs[k].Callback(result[k]);
				}

				foreach(var kv in context.ObjectValues)
					generatedClassType.InvokeMember(kv.Value.Label, BindingFlags.Static | BindingFlags.Public | BindingFlags.SetField, null, null, new object[] { kv.Key1 });

				return result;
			}
		}

		//public void Add<TDelegate>(Expression returnExpression, Action<TDelegate> callback, params Symbol[] arguments) where TDelegate : class
		//{
		//   Add(e, typeof(TDelegate), result => callback(result as TDelegate), arguments);
		//}

		public void Add<TDelegate>(Expression returnExpression, Action<TDelegate> callback, params ExpressionCompilerArgument[] arguments) where TDelegate : class
		{
			Add(returnExpression, typeof(TDelegate), result => callback(result as TDelegate), arguments);
		}

		public void Add(Expression returnExpression, Type delegateType, Action<Delegate> callback, params ExpressionCompilerArgument[] arguments)
		{
			if(!delegateType.IsSubclassOf(typeof(Delegate)))
				throw new InvalidOperationException(delegateType.Name + " is not a delegate type");

			if(arguments.Length != delegateType.GetMethod("Invoke").GetParameters().Length)
				throw new InvalidOperationException("Number of arguments is not compatible with delegate type.");

			jobs.Add(new CompilationInfo()
			{
				ReturnExpression = returnExpression,
				DelegateType = delegateType,
				Arguments = arguments,
				Callback = callback
			});
		}
	}
}
