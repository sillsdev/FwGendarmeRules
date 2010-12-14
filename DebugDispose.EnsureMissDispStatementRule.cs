// Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
// 	Copyright (c) 2010, SIL International. All Rights Reserved.
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
using System;
using System.Collections;
using System.IO;
using System.Resources;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Gendarme.Framework;
using Gendarme.Framework.Rocks;
using Gendarme.Framework.Engines;
using Gendarme.Framework.Helpers;

namespace SIL.Gendarme.Rules.DebugDispose
{
	/// <summary>
	/// This rule checks that a Disposable(bool) method contains a debug output "Missing Dispose() call"
	/// when called with parameter being false.
	/// </summary>
	/// <example>
	/// Bad example:
	/// <code>
	/// void Dispose(bool fDisposing)
	/// {
	/// }
	/// </code>
	/// </example>
	/// <example>
	/// Good example:
	/// <code>
	/// void Dispose(bool fDisposing)
	/// {
	/// 	System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
	/// }
	/// </code>
	/// </example>
	[Problem("The Dispose(bool) method doesn't contain a debug output that is triggered when called from the finalizer.")]
	[Solution("Add a 'Missing Dispose() call' output debug statement.")]
	[EngineDependency(typeof(OpCodeEngine))]
	public class EnsureMissDispStatementRule: Rule, IMethodRule
	{
		private static readonly MethodSignature Dispose = new MethodSignature("Dispose",
			"System.Void", new[] { "System.Boolean"});
		private const string ConditionalAttribute = "System.Diagnostics.ConditionalAttribute";
		private const string Debug = "System.Diagnostics.Debug";

		// note: there can be multiple [Conditional] attribute on a method
		private static bool HasConditionalAttributeForDebugging(CustomAttributeCollection cac)
		{
			foreach (CustomAttribute ca in cac)
			{
				if (ca.Constructor.DeclaringType.FullName == ConditionalAttribute)
				{
					// this should not happen since there's a single ctor accepting a string
					// but we never know what the next framework version can throw at us...
					IList cp = ca.ConstructorParameters;
					if (cp.Count < 1)
						continue;
					switch (cp[0] as string)
					{
						case "DEBUG":
						case "TRACE":
							return true;
					}
				}
			}
			return false;
		}

		// Get the store instruction associated with the load instruction
		private static Instruction GetStoreLocal(Instruction loadIns, MethodDefinition method)
		{
			Instruction storeIns = loadIns.Previous;
			do
			{
				// look for a STLOC* instruction and compare the variable indexes
				if (storeIns.IsStoreLocal() && AreMirrorInstructions(loadIns, storeIns, method))
					return storeIns;
				storeIns = storeIns.Previous;
			} while (storeIns != null);
			return null;
		}

		// Return true if both ld and st are store and load associated instructions
		private static bool AreMirrorInstructions(Instruction ld, Instruction st, MethodDefinition method)
		{
			return (ld.GetVariable(method).Index == st.GetVariable(method).Index);
		}

		private static string GetLoadStringInstruction(Instruction call, MethodDefinition method, int formatPosition)
		{
			Instruction loadString = call.TraceBack(method, -formatPosition);
			if (loadString == null)
				return null;
			
			// If we find a variable load, search the store
			while (loadString.IsLoadLocal())
			{
				Instruction storeIns = GetStoreLocal(loadString, method);
				if (storeIns == null)
					return null;
				loadString = storeIns.TraceBack(method);
				if (loadString == null)
					return null;
			}

			var mr = loadString.Operand as MethodReference;
			if (mr != null && mr.DeclaringType.FullName == "System.String")
			{
				if (mr.Name == "Concat")
				{
					return GetLoadStringInstruction(loadString, method, 0);
				}
			}

			switch (loadString.OpCode.Code)
			{
				case Code.Call:
				case Code.Callvirt:
					return GetLoadStringFromCall(loadString.Operand as MethodReference);
				case Code.Ldstr:
					return loadString.Operand as string;
				default:
					return null;
			}
		}

		private static bool IsResource(MethodDefinition method)
		{
			return method.IsStatic && method.IsGetter && method.IsGeneratedCode();
		}

		// this works because there's an earlier check limiting this to generated code
		// which is a simple, well-known, case where the first string load is known
		private static string GetResourceNameFromResourceGetter(MethodDefinition md)
		{
			foreach (Instruction instruction in md.Body.Instructions)
				if (instruction.OpCode.Code == Code.Ldstr)
					return instruction.Operand as string;
			return null;
		}

		private static EmbeddedResource GetEmbeddedResource(AssemblyDefinition ad, string resourceClassName)
		{
			ResourceCollection resources = ad.MainModule.Resources;
			foreach (EmbeddedResource resource in resources)
				if (resourceClassName.Equals(resource.Name))
					return resource;
			return null;
		}

		private static string GetLoadStringFromCall(MethodReference mr)
		{
			MethodDefinition md = mr.Resolve();
			if (md == null || !IsResource(md))
				return null;
			
			string resourceName = GetResourceNameFromResourceGetter(md);
			if (resourceName == null)
				return null;
			
			AssemblyDefinition ad = md.GetAssembly();
			string resourceClassName = md.DeclaringType.FullName + ".resources";
			EmbeddedResource resource = GetEmbeddedResource(ad, resourceClassName);
			if (resource == null)
				return null;
			
			using (MemoryStream ms = new MemoryStream(resource.Data))
				using (ResourceSet resourceSet = new ResourceSet(ms))
				{
					return resourceSet.GetString(resourceName);
				}
		}

		#region IMethodRule implementation
		public RuleResult CheckMethod(MethodDefinition method)
		{
			if (!Dispose.Matches(method))
				return RuleResult.DoesNotApply;

			if (!method.HasBody || !method.HasThis)
				return RuleResult.DoesNotApply;

			// avoid looping if we're sure there's no call in the method
			if (!OpCodeBitmask.Calls.Intersect(OpCodeEngine.GetBitmask(method)))
				return RuleResult.Failure;
			
			// it's ok if the code is not compiled for DEBUG or TRACE purposes
			if (method.HasCustomAttributes)
			{
				if (!HasConditionalAttributeForDebugging(method.CustomAttributes))
					return RuleResult.Success;
			}

			foreach (Instruction ins in method.Body.Instructions)
			{
				// look for a call...
				if (ins.OpCode.FlowControl != FlowControl.Call)
					continue;

				// ... to System.Diagnostics.Debug ...
				MethodReference mr = (ins.Operand as MethodReference);
				if (mr.DeclaringType.FullName != Debug)
					continue;
				
				// ... WriteLineIf methods
				if (mr.Name == "WriteLineIf")
				{
					ParameterDefinitionCollection parameters = mr.Parameters;
					for (int i = 0; i < parameters.Count; i++)
					{
						if (parameters[i].ParameterType.FullName == "System.String")
						{
							var output = GetLoadStringInstruction(ins, method, i);
							if (string.IsNullOrEmpty(output) || !output.ToLower().Contains("missing dispose"))
								Runner.Report(method, ins, Severity.Medium, Confidence.Normal);
							return Runner.CurrentRuleResult;
						}
					}
				}
			}

			// Didn't find a WriteLineIf
			Runner.Report(method, Severity.Medium, Confidence.High);
			return RuleResult.Failure;
		}
		#endregion
	}
}

