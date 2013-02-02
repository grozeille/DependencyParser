using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Mono.Cecil;

namespace DependencyParser {
	

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class Lcom4Writer {

		private readonly ReferenceComparer comparer = new ReferenceComparer();

		public void Write(XmlTextWriter xml, TypeDefinition type, IEnumerable<IEnumerable<MemberReference>> blocks)
		{
			xml.WriteStartElement("type");
			xml.WriteAttributeString("fullName", type.FullName);
			foreach (var block in blocks)
			{
				var orderedBlock = block.OrderBy(x => x, comparer);

				xml.WriteStartElement("block");
				foreach (var memberReference in orderedBlock)
				{
					xml.WriteStartElement("element");
					var method = memberReference as MethodDefinition;
					string name;
					string elementType;
					if (method == null)
					{
						elementType = "Field";
						name = BuildFieldName(memberReference as FieldDefinition);
					} else
					{
						elementType = "Method";
						name = BuildSignature(method);
					}
					xml.WriteAttributeString("type", elementType);
					xml.WriteAttributeString("name", name);
					xml.WriteEndElement();
				}
				xml.WriteEndElement();
			}
			xml.WriteEndElement();
		}

		private string BuildFieldName(FieldDefinition field)
		{
			string name;
			if (field.Name.Contains("BackingField"))
			{
				name = field.Name.Split(new string[] { "<", ">" }, StringSplitOptions.RemoveEmptyEntries)[0] + " (property)";
			} else
			{
				name = field.Name;
			}
			return name;
		}

		private string BuildSignature(MethodDefinition mth)
		{
			var builder = new StringBuilder();
			builder.Append(mth.ReturnType);
			builder.Append(" ");
			builder.Append(mth.Name);
			builder.Append("(");
			var parameters = mth.Parameters;
			for (int i = 0; i < parameters.Count; i++)
			{
				builder.Append(parameters[i].ParameterType.FullName);
				if (i < (parameters.Count-1))
				{
					builder.Append(", ");
				}
			}
			builder.Append(")");
			return builder.ToString();
		}

		private class ReferenceComparer : IComparer<MemberReference>
		{
			public int Compare(MemberReference ref1, MemberReference ref2)
			{
				if (ref1 is MethodDefinition && ref2 is FieldDefinition)
				{
					return 1;
				}
				if (ref2 is MethodDefinition && ref1 is FieldDefinition) {
					return -1;
				}

				return string.Compare(ref1.Name, ref2.Name);
			}
		}
	}
}
