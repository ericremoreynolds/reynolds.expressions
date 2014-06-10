using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CSharp;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;

namespace Reynolds.Expressions
{
	public interface ICodeGenerationContext
	{
		ICodeGenerationContext Emit(Expression e);
		ICodeGenerationContext Emit(Type t);
		ICodeGenerationContext Emit(double d);
		ICodeGenerationContext Emit(int i);
		ICodeGenerationContext Emit(string s);
		ICodeGenerationContext Emit(object o);
		ICodeGenerationContext Emit(Expression e, Expression[] args);
	}

	public class CodeGenerationContext : ICodeGenerationContext, IDisposable
	{
		CSharpCodeProvider provider = new CSharpCodeProvider();
		StringBuilder sb = new StringBuilder();
		public readonly Dictionary<object, string> ObjectValues = new Dictionary<object, string>();
		public readonly HashSet<Assembly> Assemblies = new HashSet<Assembly>();

		public ICodeGenerationContext Emit(Type type)
		{
			Assemblies.Add(type.Assembly);
			sb.Append(provider.GetTypeOutput(new CodeTypeReference(type)));
			return this;
		}

		public ICodeGenerationContext Emit(object obj)
		{
			string label;
			if(!ObjectValues.TryGetValue(obj, out label))
				ObjectValues[obj] = label = "v" + ObjectValues.Count.ToString();
			sb.Append(label);
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

	public class ExpressionCompiler
	{
		class CompilationInfo
		{
			public Expression Expression;
			public Type DelegateType;
			public Symbol[] Arguments;
		}

		List<CompilationInfo> jobs = new List<CompilationInfo>();

		public Delegate[] CompileAll()
		{
			using(var context = new CodeGenerationContext())
			{
				context.Emit("using System;");
				context.Emit("public static class GeneratedFunction {");

				for(int k = 0; k < jobs.Count; k++)
				{
					var mi = jobs[k].DelegateType.GetMethod("Invoke");
					var args = mi.GetParameters();

					ExpressionSubstitution[] subs = new ExpressionSubstitution[jobs[k].Arguments.Length];
					for(int j = 0; j < jobs[k].Arguments.Length; j++)
						subs[j] = jobs[k].Arguments[j] | new Symbol("x" + j.ToString());
					var e = jobs[k].Expression.Substitute(subs);

					context.Emit("public static ").Emit(mi.ReturnType).Emit(" f").Emit(k).Emit("(");

					for(int j = 0; j < args.Length; j++)
					{
						if(j > 0)
							context.Emit(", ");
						context.Emit(args[j].ParameterType).Emit(" x").Emit(j);
					}
					context.Emit(") { return ").Emit(e).Emit("; }");
				}

				foreach(var kv in context.ObjectValues)
					context.Emit("public static ").Emit(kv.Key.GetType()).Emit(" ").Emit(kv.Value).Emit(";");

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
				}

				foreach(var kv in context.ObjectValues)
					generatedClassType.InvokeMember(kv.Value, BindingFlags.Static | BindingFlags.Public | BindingFlags.SetField, null, null, new object[] { kv.Key });

				return result;
			}
		}

		public void Add(Expression e, Type delegateType, params Symbol[] arguments)
		{
			if(!delegateType.IsSubclassOf(typeof(Delegate)))
				throw new InvalidOperationException(delegateType.Name + " is not a delegate type");

			if(arguments.Length != delegateType.GetMethod("Invoke").GetParameters().Length)
				throw new InvalidOperationException("Number of arguments is not compatible with delegate type.");

			jobs.Add(new CompilationInfo()
			{
				Expression = e,
				DelegateType = delegateType,
				Arguments = arguments
			});
		}
	}
}
