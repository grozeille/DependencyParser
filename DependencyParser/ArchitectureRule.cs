using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace DependencyParser {

	public class ArchitectureRule {

		public string FromPattern { get; set; }

		public string ToPattern { get; set; }

		public bool IsFromTypeApplies(TypeReference type)
		{
			return WildcardPatternMatcher.MatchWildcardString(FromPattern, type.FullName);
		}

		public bool IsToTypeApplies(TypeReference type)
		{
			return WildcardPatternMatcher.MatchWildcardString(ToPattern, type.FullName);
		}
	}
}
