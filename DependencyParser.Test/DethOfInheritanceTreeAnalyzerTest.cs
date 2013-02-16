using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil;
using NUnit.Framework;

namespace DependencyParser.Test {

	[NUnit.Framework.TestFixture]
	public class DethOfInheritanceTreeAnalyzerTest {

		[Test]
		public void Should_Compute_DIT()
		{
			var t = typeof(DethOfInheritanceTreeAnalyzerTest).GetCecilType();
			var result = new DethOfInheritanceTreeAnalyzer().ComputeDIT(t);
			Assert.AreEqual(1, result);
		}
	}
}
