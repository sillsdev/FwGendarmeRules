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
	interface Interface: IDisposable
	{
	}

	class WithoutDispose
	{
		public WithoutDispose() {}
	}

	class WithDisposeNoFinalizer: IDisposable
	{
		public WithDisposeNoFinalizer() {}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool fDisposing)
		{}
	}

	class WithFinalizer: IDisposable
	{
		public WithFinalizer() {}
		~WithFinalizer() {}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool fDisposing)
		{}
	}

	class WithoutDisposeBool: IDisposable
	{
		public void Dispose() {}
	}

	[TestFixture]
	public class EnsureDebugDisposeFinalizerTests : TypeRuleTestFixture<EnsureDebugDisposeFinalizerRule>
	{
		/// <summary>
		/// Tests that the rule doesn't apply for a class that doesn't derive from IDisposable
		/// </summary>
		[Test]
		public void ClassWithoutDispose()
		{
			AssertRuleDoesNotApply<WithoutDispose>();
		}

		/// <summary>
		/// Tests that the rule applies and succeeds for a class that derives from IDisposable
		/// and that implements a finalizer.
		/// </summary>
		[Test]
		public void ClassWithFinalizer()
		{
			AssertRuleSuccess<WithFinalizer>();
		}

		/// <summary>
		/// Tests that the rule applies and fails for a class that derives from IDisposable
		/// but doesn't implement a finalizer.
		/// </summary>
		[Test]
		public void ClassWithoutFinalizer()
		{
			AssertRuleFailure<WithDisposeNoFinalizer>();
		}

		/// <summary>
		/// Tests that the rule does not apply on interfaces
		/// </summary>
		[Test]
		public void DoesntApplyOnInterface()
		{
			AssertRuleDoesNotApply<Interface>();
		}

		/// <summary>
		/// Tests that in a derived class we don't expect the "Missing Dispose" output because
		/// that should be done in the base class.
		/// </summary>
		[Test]
		public void DoesntApplyInDerivedClass()
		{
			AssertRuleDoesNotApply<Derived>();
		}

		/// <summary>
		/// Tests that we expect the "Missing Dispose" output in a derived class that implements
		/// IDisposable whereas the parent class doesn't.
		/// </summary>
		[Test]
		public void AppliesInDerivedClassThatImplementsIDisposable()
		{
			AssertRuleFailure<DisposableA>();
		}

		/// <summary>
		/// Tests that the rule doesn't apply in a class that derives from a class whose ancestor
		/// class implements IDisposable.
		/// </summary>
		[Test]
		public void DoesntApplyInDoubleDerivedClass()
		{
			AssertRuleDoesNotApply<DerivedDerived>();
		}

		/// <summary>
		/// Tests that the rule doesn't apply in a class that implements IDisposable but
		/// doesn't have a method Dispose(bool)
		/// </summary>
		[Test]
		public void DoesntApplyWithoutDisposeBoolMethod()
		{
			AssertRuleDoesNotApply<WithoutDisposeBool>();
		}
	}
}

