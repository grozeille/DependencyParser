using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace DependencyParser {


	public class ArchitectureRuleEngine {

		private IEnumerable<ArchitectureRule> rules = Enumerable.Empty<ArchitectureRule>();

		public void Init(string s)
		{
			if (s == null) {
				return;
			}
			var fromToPairs = s.Split(',');

			rules =
				from pair in fromToPairs
				select new ArchitectureRule() {
					FromPattern = pair.Split(':')[0],
					ToPattern = pair.Split(':')[1]
				};
		}

		public IEnumerable<ArchitectureViolation> FindArchitectureViolation(TypeDefinition fromType, IEnumerable<TypeReference> toTypes)
		{
			var applicableRules =
				from r in rules
				where r.IsFromTypeApplies(fromType)
				select r;

			var badDependencies =
				from d in toTypes
				from r in applicableRules
				where r.IsToTypeApplies(d)
				select new ArchitectureViolation() {Subject = fromType, Dependency = d, Rule = r};

			return badDependencies.Distinct(new ViolationEqualityComparer());
		}
	}

	public class ViolationEqualityComparer : IEqualityComparer<ArchitectureViolation> {
		public bool Equals(ArchitectureViolation v1, ArchitectureViolation v2)
		{
			return v1.Dependency.FullName == v2.Dependency.FullName;
		}

		public int GetHashCode(ArchitectureViolation v)
		{
			return v.Dependency.FullName.GetHashCode();
		}
	}
}
