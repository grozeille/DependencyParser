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

		public void Write(XmlTextWriter xml, TypeDefinition type, IEnumerable<IEnumerable<MemberReference>> blocks)
		{
			xml.WriteStartElement("type");
			xml.WriteAttributeString("fullName", type.FullName);
			foreach (var block in blocks)
			{
				xml.WriteStartElement("block");
				foreach (var memberReference in block)
				{
					xml.WriteStartElement("element");
					var method = memberReference as MethodDefinition;
					string name;
					string elementType;
					if (method == null)
					{
						elementType = "Field";
						name = memberReference.Name;
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
	}
}
