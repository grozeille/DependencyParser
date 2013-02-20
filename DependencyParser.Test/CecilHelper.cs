using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil;

namespace DependencyParser.Test {

	public static class CecilHelper {
		
		public static TypeDefinition GetCecilType(this Type t)
		{
			string name = t.FullName;
			string unit = Assembly.GetExecutingAssembly().Location;
			var assembly = AssemblyDefinition.ReadAssembly(unit);
			return assembly.MainModule.GetType(name);
		}
	}
}
