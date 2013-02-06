using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Mono.Cecil;
using NUnit.Framework;

namespace DependencyParser.Test {

	/// <summary>
	/// Tests inspired from the following thread in sonar mailing list:
	/// http://sonar.15.n6.nabble.com/Calculation-of-Response-For-Class-metric-td5000313.html
	/// </summary>
	[NUnit.Framework.TestFixture]
	public class ResponseForClassAnalyzerTest {

		[Test]
		public void Should_Compute_RFC()
		{
			var analyzer = new ResponseForClassAnalyzer();
			
			Assert.AreEqual(6, analyzer.ComputeRFC(GetType("DependencyParser.Test.ClassA")));
		}

		[Test]
		public void Should_Ignore_Property_Accessors()
		{
			var analyzer = new ResponseForClassAnalyzer();

			Assert.AreEqual(1, analyzer.ComputeRFC(GetType("DependencyParser.Test.Country")));
		}

		[Test]
		public void Should_Take_In_Account_Computed_Properties()
		{
			var analyzer = new ResponseForClassAnalyzer();

			Assert.AreEqual(3, analyzer.ComputeRFC(GetType("DependencyParser.Test.Employee")));
		}

		private TypeDefinition GetType(string name)
		{
			string unit = Assembly.GetExecutingAssembly().Location;
			var assembly = AssemblyDefinition.ReadAssembly(unit);
			return assembly.MainModule.GetType(name);
		}


	}

	public class ClassA {
		private ClassB classB = new ClassB();
		public void DoSomething(){
			Console.WriteLine("doSomething");
		}
		public void DoSomethingBasedOnClassB(){
			Console.WriteLine(classB.ToString());
		}
	}

	public class ClassB {
		private ClassA classA = new ClassA();
		public void DoSomethingBasedOneClassA(){
			Console.WriteLine(classA.ToString());
		}

		public string ToString()
		{
			return "classB";
		}
	}

	public class Country {
		public string Code { get; set; }
		public double Vat { get; set; }
	}

	public class Employee {

		public double Salary { get; set; }
		public double Bonus { get; set; }
		public int YearsInManagement { get; set; }
		public int YearsInSales { get; set; }
		public double TotalEarnings
		{
			get
			{
				return Salary + Bonus;	
			}
			
		}
		public int TotalYearsOfService
		{
			get
			{
				return YearsInManagement + YearsInSales;	
			}
		}
	}
}
