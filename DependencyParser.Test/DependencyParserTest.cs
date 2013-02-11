using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace DependencyParser.Test {

	[NUnit.Framework.TestFixture]
	public class DependencyParserTest {

		[Test]
		public void Should_Create_An_XML_Report()
		{
			File.Delete("test.xml");
			string assemblyPath = @"C:\work\temp\springdotnet\trunk\build\VS.Net.2010\Spring.Core\Debug\Spring.Core.dll";
			//Program.Main(new string[] { "-a=testdata/Example.Core.dll", "-o=test.xml" });
			Program.Main(new string[] { "-a="+assemblyPath, "-o=test-spring.xml" });
			string expected = File.ReadAllText("testdata/test.xml");
			string result = File.ReadAllText("test.xml");

			Assert.AreEqual(expected, result);


		}
	}
}
