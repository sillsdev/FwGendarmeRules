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
	class NonDisposable
	{
		public void Dispose(bool fDispose)
		{}
	}

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

	class Derived: WithStatement
	{
		protected override void Dispose(bool fDisposing)
		{
			base.Dispose(fDisposing);
		}
	}

	class DerivedDerived : Derived
	{
		protected override void Dispose(bool fDisposing)
		{
			base.Dispose(fDisposing);
		}
	}

	class DerivedControl: System.Windows.Forms.Control
	{
		protected override void Dispose(bool release_all)
		{
			base.Dispose(release_all);
		}
	}

	class OtherDerivedControl: System.Windows.Forms.DataGridViewColumn
	{
		protected override void Dispose(bool release_all)
		{
			base.Dispose(release_all);
		}
	}

	class A
	{}

	class DisposableA: A, IDisposable
	{
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool fDisposing)
		{

		}
	}

	[TestFixture]
	public class EnsureDebugDisposeMissDispStatementTests: MethodRuleTestFixture<EnsureDebugDisposeMissDispStatementRule>
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
			AssertRuleFailure<NonstandardText>("Dispose", new[] { typeof(bool) });
		}

		/// <summary>
		/// Tests that in a derived class we don't expect the "Missing Dispose" output because
		/// that should be done in the base class.
		/// </summary>
		[Test]
		public void DisposeInDerivedClass()
		{
			AssertRuleDoesNotApply<Derived>("Dispose", new[] { typeof(bool) });
		}

		/// <summary>
		/// Tests that we expect the "Missing Dispose" output in a class that derives directly
		/// from Control, UserControl or Form.
		/// </summary>
		[Test]
		public void DisposeInDerivedControl()
		{
			AssertRuleFailure<DerivedControl>("Dispose", new[] { typeof(bool) });
		}

		/// <summary>
		/// Tests that we expect the "Missing Dispose" output in a class that derives directly
		/// from a class in System.Windows.Forms.
		/// </summary>
		[Test]
		public void DisposeInOtherDerivedControl()
		{
			AssertRuleFailure<OtherDerivedControl>("Dispose", new[] { typeof(bool) });
		}

		/// <summary>
		/// Tests that we expect the "Missing Dispose" output in a derived class that implements
		/// IDisposable whereas the parent class doesn't.
		/// </summary>
		[Test]
		public void DisposeInDerivedClassThatImplementsIDisposable()
		{
			AssertRuleFailure<DisposableA>("Dispose", new[] { typeof(bool) });
		}

		/// <summary>
		/// Tests that the rule doesn't apply in a class that derives from a class whose ancestor
		/// class implements IDisposable.
		/// </summary>
		[Test]
		public void DisposeInDoubleDerivedClass()
		{
			AssertRuleDoesNotApply<DerivedDerived>("Dispose", new[] { typeof(bool) });
		}

		/// <summary>
		/// Tests that the rule doesn't apply in a class that doesn't derive from IDisposable
		/// and that doesn't have any parent class.
		/// </summary>
		[Test]
		public void NonDisposable()
		{
			AssertRuleDoesNotApply<NonDisposable>("Dispose", new[] { typeof(bool) });
		}
	}
}

