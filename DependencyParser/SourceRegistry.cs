using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace DependencyParser {


	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class SourceRegistry {

		private HashSet<string> multiTypeSourceFiles = new HashSet<string>();

		public SourceRegistry(ModuleDefinition module)
		{
			var registry = new Dictionary<string, IEnumerable<TypeDefinition>>();
			foreach (var t in module.Types) {
				var path = t.GetSourcePath();
				if (path == null)
				{
					continue;
				}
				if (registry.ContainsKey(path))
				{
					var types = registry[path];
					if (types is List<TypeDefinition>)
					{
						((List<TypeDefinition>)types).Add(t);
					} else
					{
						var typeList = new List<TypeDefinition>(types) {t};
						registry.Remove(path);
						registry.Add(path, typeList);
					}
				} else
				{
					registry.Add(path, new TypeDefinition[] { t });
				}
			}
			foreach (var pair in registry.Where(entry => entry.Value.Count() > 1))
			{
				multiTypeSourceFiles.Add(pair.Key);
			} 
		}

		public bool IsMultiTypeFile(string path)
		{

			return path==null ? false : multiTypeSourceFiles.Contains(path);
		}

		

	}
}
