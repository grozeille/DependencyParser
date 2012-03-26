using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace DependencyParser.Test {

	[NUnit.Framework.TestFixture]
	class DependencyParserTest {

		[Test]
		public void Should_Create_An_XML_Report()
		{
			File.Delete("test.xml");
			Program.Main(new string[] { "-a=testdata/Example.Core.dll", "-o=test.xml" });
			string expected = File.ReadAllText("testdata/test.xml");
			string result = File.ReadAllText("test.xml");

			Assert.AreEqual(expected, result);


		}
	}
}
