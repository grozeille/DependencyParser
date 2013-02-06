using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace DependencyParser {

	public class DethOfInheritanceTreeAnalyzer {

		public int ComputeDIT(TypeDefinition t) 
		{
			var currentType = t;
			var result = 0;
			do
			{
				if (currentType.BaseType == null)
				{
					currentType = null;
				}
				else
				{
					result++;
					currentType = currentType.BaseType.Resolve();
				}
			} while (currentType != null);

			return result;
		}
	}
}
