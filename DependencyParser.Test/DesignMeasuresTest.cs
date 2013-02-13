using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Mono.Cecil;
using NUnit.Framework;

namespace DependencyParser.Test {

	[NUnit.Framework.TestFixture]
	public class DesignMeasuresTest {
		[Test]
		public void Should_Generate()
		{
			File.Delete("lcom4-generated.xml");
			using (var stream = new FileStream("lcom4-generated.xml", FileMode.Create))
			{
				using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
				{
					writer.Formatting = Formatting.Indented;
					var designWriter = new DesignMeasures();
					var typeDefinition = GetType(typeof(SimpleClassWithTwoFields));
					var blocks = new HashSet<HashSet<MemberReference>>();
				
					foreach (var mth in typeDefinition.Methods)
					{
						var block = new HashSet<MemberReference> { mth, typeDefinition.Fields.First() };
						blocks.Add(block);
					}
					designWriter.Type = typeDefinition;
					designWriter.Lcom4Blocks = blocks;
					designWriter.ResponseForClass = 42;
					designWriter.DethOfInheritance = 17;

					designWriter.Write(writer);
				}
			}
			string expected = File.ReadAllText("lcom4-expected.xml");
			string result = File.ReadAllText("lcom4-generated.xml");

			Assert.AreEqual(expected, result);
		}

		private TypeDefinition GetType(Type t)
		{
			string name = t.FullName;
			string unit = Assembly.GetExecutingAssembly().Location;
			var assembly = AssemblyDefinition.ReadAssembly(unit);
			return assembly.MainModule.GetType(name);
		}
	}

	public class SimpleClassWithTwoFields {
		private int fieldA;

		private int fieldB;

		public void doA()
		{
			fieldA++;
		}

		public void doB()
		{
			fieldB++;
		}
	}
}
