// Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
// 	Copyright (c) 2010, SIL International. All Rights Reserved.
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>

using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

using Gendarme.Framework;
using Gendarme.Framework.Rocks;
using Gendarme.Framework.Engines;
using Gendarme.Framework.Helpers;

namespace SIL.Gendarme.Rules.DebugDispose
{
	/// <summary>
	/// This rule will fire for types which implement <c>System.IDisposable</c> but do not define a finalizer.
	/// </summary>
	/// <example>
	/// Bad example:
	/// <code>
	/// class NoFinalizer: IDisposable
	/// {
	/// public void Dispose()
	/// {
	///     Dispose(true);
	///     GC.SuppressFinalize(this);
	/// }
	/// }
	/// </code>
	/// </example>
	/// <example>
	/// Good example:
	/// <code>
	/// class HasFinalizer: IDisposable
	/// {
	/// #IF DEBUG
	///	~HasFinalizer ()
	///	{
	///     Dispose(false)
	///	}
	/// #endif
	/// public void Dispose()
	/// {
	///     Dispose(true);
	///     GC.SuppressFinalize(this);
	/// }
	/// protected virtual void Dispose(bool disposing)
	/// {
	///     System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
	/// }
	/// }
	/// </code>
	/// </example>
	[Problem ("This type implements IDisposable but doesn't have a finalizer.")]
	[Solution ("When debugging dispose issues add a finalizer, calling Dispose(false), to trigger debug output.")]
	public class EnsureFinalizer: Rule, ITypeRule
	{
		#region ITypeRule implementation
		public RuleResult CheckType(TypeDefinition type)
		{
			// rule applies only to types and interfaces
			if (type.IsEnum || type.IsDelegate() || type.IsGeneratedCode() || type.IsValueType)
				return RuleResult.DoesNotApply;
			
			// rule only applies to type that implements IDisposable
			if (!type.Implements("System.IDisposable"))
				return RuleResult.DoesNotApply;
			
			// no problem if a finalizer is found
			if (type.HasMethod(MethodSignatures.Finalize))
				return RuleResult.Success;

			Runner.Report(type, Severity.Medium, Confidence.High);
			return RuleResult.Failure;
		}
		#endregion
	}
}

