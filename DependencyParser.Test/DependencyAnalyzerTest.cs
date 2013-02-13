using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil;
using NUnit.Framework;

namespace DependencyParser.Test {


	[NUnit.Framework.TestFixture]
	public class DependencyAnalyzerTest {

		private DependencyAnalyzer analyzer;


		[SetUp]
		public void SetUp()
		{
			analyzer = new DependencyAnalyzer();
		}

		[Test]
		public void Should_Find_Dependencies_From_Attributes()
		{
			var dependencies = analyzer.FindTypeDependencies(GetType(this.GetType()));
			Assert.AreEqual(1, dependencies.Count(n => n.FullName == typeof(TestAttribute).FullName));
		}

		[Test]
		public void Should_Find_Dependencies_From_Fields()
		{
			var dependencies = analyzer.FindTypeDependencies(GetType(this.GetType()));
			Assert.AreEqual(1, dependencies.Count(n => n.FullName == typeof(DependencyAnalyzer).FullName));
		}

		[Test]
		public void Should_Find_Dependencies_From_Properties()
		{
			var dependencies = analyzer.FindTypeDependencies(GetType(typeof(ClassWithProperty)));
			Assert.IsTrue(dependencies.Contains(GetType(typeof(Thread))));
		}

		[Test]
		public void Should_Find_Dependencies_From_Parameters()
		{
			var dependencies = analyzer.FindTypeDependencies(GetType(typeof(ClassWithCallsOnParameters)));
			Assert.IsTrue(dependencies.Contains(GetType(typeof(Thread))));
		}

		[Test]
		public void Should_Find_Dependencies_From_Array()
		{
			var dependencies = analyzer.FindTypeDependencies(GetType(typeof(ClassWithArray)));
			Assert.IsTrue(dependencies.Contains(GetType(typeof(Thread))));
		}

		[Test]
		public void Should_Ignore_System_Dependencies()
		{
			var dependencies = analyzer.FindTypeDependencies(GetType(typeof(ClassWithSystemDependency)));
			Assert.AreEqual(0, dependencies.Count(n => n.FullName.Contains("Dictionary")));
		}

		private TypeDefinition GetType(Type t)
		{
			string name = t.FullName;
			string unit = Assembly.GetExecutingAssembly().Location;
			var assembly = AssemblyDefinition.ReadAssembly(unit);
			return assembly.MainModule.GetType(name);
		}


	}

	public class ClassWithProperty {

		public Thread MyThread { get; set; }
	}

	public abstract class ClassWithCallsOnParameters {

		public void Stop(Thread thread)
		{
		}

		public void Start(Thread thread)
		{
		}

	}

	public abstract class ClassWithArray {
		public void Stop(Thread[] threads)
		{
		}
	}

	public abstract class ClassWithSystemDependency {
		
		private IDictionary<string, string> dic = new Dictionary<string, string>();

		public void Stop(Thread[] threads)
		{
		}
	}

	public class Thread
	{
		
	}
}
