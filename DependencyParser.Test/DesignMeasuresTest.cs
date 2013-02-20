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
		public void Should_Write_Correct_XML()
		{
			File.Delete("lcom4-generated.xml");
			using (var stream = new FileStream("lcom4-generated.xml", FileMode.Create))
			{
				using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
				{
					writer.Formatting = Formatting.Indented;
					var measure = new DesignMeasures();
					var typeDefinition = typeof(SimpleClassWithTwoFields).GetCecilType();
					var blocks = new HashSet<HashSet<MemberReference>>();
				
					foreach (var mth in typeDefinition.Methods)
					{
						var block = new HashSet<MemberReference> { mth, typeDefinition.Fields.First() };
						blocks.Add(block);
					}
					measure.Type = typeDefinition;
					measure.Lcom4Blocks = blocks;
					measure.ResponseForClass = 42;
					measure.DethOfInheritance = 17;

					measure.Write(writer);
				}
			}
			string expected = File.ReadAllText("lcom4-expected.xml");
			string result = File.ReadAllText("lcom4-generated.xml");

			Assert.AreEqual(expected, result);
		}

		[Test]
		public void Should_Merge_Measures()
		{
			File.Delete("types-merged-generated.xml");
			var measure = new DesignMeasures();
			var typeDefinition = typeof(SimpleClassWithTwoFields).GetCecilType();
			measure.Type = typeDefinition;
			measure.ResponseForClass = 42;
			measure.DethOfInheritance = 17;
			measure.Lcom4Blocks = Enumerable.Empty<IEnumerable<MemberReference>>();

			var measure2 = new DesignMeasures();
			var typeDefinition2 = typeof(DesignMeasuresTest).GetCecilType();
			measure2.Type = typeDefinition2;
			measure2.ResponseForClass = 36;
			measure2.DethOfInheritance = 42;
			measure2.Lcom4Blocks = Enumerable.Empty<IEnumerable<MemberReference>>();

			var mergedMeasures = measure.Merge(measure2);
			using (var stream = new FileStream("types-merged-generated.xml", FileMode.Create))
			{
				using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
				{
					writer.Formatting = Formatting.Indented;
					mergedMeasures.Write(writer);
				}
			}

			string expected = File.ReadAllText("types-merged-expected.xml");
			string result = File.ReadAllText("types-merged-generated.xml");
			Assert.AreEqual(expected, result);

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
