// Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
// 	Copyright (c) 2010, SIL International. All Rights Reserved.
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
using System;
using NUnit.Framework;

using Gendarme.Framework.Rocks;

using Test.Rules.Definitions;
using Test.Rules.Fixtures;
using Test.Rules.Helpers;

namespace SIL.Gendarme.Rules.DebugDispose
{
	abstract class NoBody: IDisposable
	{
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public abstract void Dispose(bool fDispose);
	}

	class StaticDispose: IDisposable
	{
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public static void Dispose(bool fDisposing)
		{}
	}

	class MissingStatement: IDisposable
	{
		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~MissingStatement()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
		}
		#endregion
	}

	class WithStatement : IDisposable
	{
		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~WithStatement()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().ToString() + " *******");
		}
		#endregion
	}

	class NonstandardText : IDisposable
	{
		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "Just some gibberish");
		}
	}

	[TestFixture]
	public class EnsureMissDispStatementTests: MethodRuleTestFixture<EnsureMissDispStatementRule>
	{
		/// <summary>
		/// Tests that the rule doesn't apply when the method doesn't have a body
		/// </summary>
		[Test]
		public void DoesntApplyWithoutBody()
		{
			AssertRuleDoesNotApply<NoBody>("Dispose", new[] { typeof(bool) });
		}

		/// <summary>
		/// Tests that the rule doesn't apply on a static method
		/// </summary>
		[Test]
		public void DoesntApplyWithStaticMethod()
		{
			AssertRuleDoesNotApply<StaticDispose>("Dispose", new[] { typeof(bool) });
		}

		/// <summary>
		/// Tests that the rule applies and fails for a Dispose(bool) method that doesn't
		/// add a "Missing Dispose" output if called with !fDisposing
		/// </summary>
		[Test]
		public void DisposeWithMissingDebugStatement()
		{
			AssertRuleFailure<MissingStatement>("Dispose", new[] { typeof(bool) });
		}

		/// <summary>
		/// Tests that the rule applies and succeeds for a Dispose(bool) method that does
		/// add a "Missing Dispose" output if called with !fDisposing
		/// </summary>
		[Test]
		public void DisposeWithDebugStatement()
		{
			AssertRuleSuccess<WithStatement>("Dispose", new[] { typeof(bool) });
		}

		/// <summary>
		/// Tests that the rule applies and fails for a Dispose(bool) method that has a
		/// non-standard text.
		/// </summary>
		[Test]
		public void DisposeWithNonStandardText()
		{
			AssertRuleFailure<NonstandardText>("Dispose", new[] { typeof(bool)});
		}
	}
}

