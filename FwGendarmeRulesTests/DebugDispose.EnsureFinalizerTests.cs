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
	class WithoutDispose
	{
		public WithoutDispose() {}
	}

	class WithDisposeNoFinalizer: IDisposable
	{
		public WithDisposeNoFinalizer() {}
		public void Dispose() {}
	}

	class WithFinalizer: IDisposable
	{
		public WithFinalizer() {}
		~WithFinalizer() {}
		public void Dispose() {}
	}

	[TestFixture]
	public class EnsureFinalizerTests : TypeRuleTestFixture<EnsureFinalizer>
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
	}
}

