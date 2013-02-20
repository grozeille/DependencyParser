using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil;
using NUnit.Framework;

namespace DependencyParser.Test {


	[NUnit.Framework.TestFixture]
	public class ArchitectureRuleEngineTest {

		private ArchitectureRuleEngine engine;

		
		[SetUp]
		public void SetUp()
		{
			engine = new ArchitectureRuleEngine() {};
		}

		[Test]
		public void Should_Find_Forbidden_Dependencies()
		{
			engine.Init("*:DependencyParser.*");
			var dependencies = new TypeReference[] { typeof(Foo).GetCecilType() };
			var result =
				engine.FindArchitectureViolation(typeof(Bar).GetCecilType(), dependencies);
			Assert.AreEqual(1, result.Count());
			Assert.AreEqual("DependencyParser.Test.Foo", result.First().Dependency.FullName);
		}

		[Test]
		public void Should_Not_Used_Unrelated_Rules_For_Source_Types()
		{
			engine.Init("Xyz.*:DependencyParser.*");
			var dependencies = new TypeReference[] { typeof(Foo).GetCecilType() };
			var result =
				engine.FindArchitectureViolation(typeof(Bar).GetCecilType(), dependencies);
			Assert.AreEqual(0, result.Count());
		}

		[Test]
		public void Should_Not_Used_Unrelated_Rules_For_Destination_Types()
		{
			engine.Init("DependencyParser.*:xyz.*");
			var dependencies = new TypeReference[] { typeof(Foo).GetCecilType() };
			var result =
				engine.FindArchitectureViolation(typeof(Bar).GetCecilType(), dependencies);
			Assert.AreEqual(0, result.Count());
		}

		[Test]
		public void Should_Not_Report_Twice_The_Same_Forbidden_Dependency()
		{
			engine.Init("*:DependencyParser.*,*:DependencyParser.Test.*");
			var dependencies = new TypeReference[] { typeof(Foo).GetCecilType() };
			var result =
				engine.FindArchitectureViolation(typeof(Bar).GetCecilType(), dependencies);
			Assert.AreEqual(1, result.Count());
			Assert.AreEqual("DependencyParser.Test.Foo", result.First().Dependency.FullName);
		}
		
	}

	public class Foo
	{
		
	}

	public class Bar {

	}

}
