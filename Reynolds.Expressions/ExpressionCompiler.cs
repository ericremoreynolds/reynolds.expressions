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
			using(var provider = new CSharpCodeProvider())
			{
				HashSet<Assembly> assemblies = new HashSet<Assembly>();
				assemblies.Add(Assembly.GetAssembly(typeof(object)));

				StringBuilder sb = new StringBuilder();
				sb.Append("using System;");
				sb.Append("public static class GeneratedFunction {");

				for(int k = 0; k < jobs.Count; k++)
				{
					var mi = jobs[k].DelegateType.GetMethod("Invoke");
					var args = mi.GetParameters();

					ExpressionSubstitution[] subs = new ExpressionSubstitution[jobs[k].Arguments.Length];
					for(int j = 0; j < jobs[k].Arguments.Length; j++)
						subs[j] = jobs[k].Arguments[j] | new Symbol("x" + j.ToString());
					var e = jobs[k].Expression.Substitute(subs);

					assemblies.Add(mi.ReturnType.Assembly);

					sb.Append("public static " + provider.GetTypeOutput(new CodeTypeReference(mi.ReturnType)) + " f" + k.ToString() + "(");

					for(int j = 0; j < args.Length; j++)
					{
						sb.Append(j > 0 ? ", " : "").Append(provider.GetTypeOutput(new CodeTypeReference(args[j].ParameterType))).Append(" x").Append(j);
						assemblies.Add(args[j].ParameterType.Assembly);
					}
					sb.Append(") { return " + e.ToCode() + "; }");
				}

				sb.Append("}");
				string code = sb.ToString();

				CompilerParameters parameters = new CompilerParameters();
				parameters.GenerateInMemory = true;
				parameters.TreatWarningsAsErrors = false;
				parameters.GenerateExecutable = false;
				parameters.CompilerOptions = "/optimize";
				parameters.IncludeDebugInformation = false;
				foreach(var assembly in assemblies)
					parameters.ReferencedAssemblies.Add(assembly.Location);

				CompilerResults results = new CSharpCodeProvider().CompileAssemblyFromSource(parameters, new string[] { code });

				if(results.Errors.HasErrors)
					throw new Exception("Compile error: " + results.Errors[0].ToString()); //, results.Errors);

				Delegate[] result = new Delegate[jobs.Count];
				for(int k = 0; k < jobs.Count; k++)
				{
					var mi = results.CompiledAssembly.GetModules()[0].GetType("GeneratedFunction").GetMethod("f" + k.ToString());
					result[k] = Delegate.CreateDelegate(jobs[k].DelegateType, mi);
				}

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
