using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Mono.Cecil;

namespace DependencyParser {

	public class ArchitectureViolation {

		public ArchitectureRule Rule { get; set; }

		public TypeDefinition Subject { get; set; }

		public TypeReference Dependency { get; set;}

		public void Write(XmlTextWriter xml)
		{
			xml.WriteStartElement("Violation");
			xml.WriteAttributeString("fullname", Subject.FullName);
			var path = Subject.GetSourcePath();
			if (path != null) {
				xml.WriteAttributeString("path", path);
			}
			xml.WriteAttributeString("fromPattern", Rule.FromPattern);
			xml.WriteAttributeString("toPattern", Rule.ToPattern);
			xml.WriteAttributeString("dependency", Dependency.FullName);
			xml.WriteEndElement();

		}
	}
}
