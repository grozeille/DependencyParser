using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gendarme.Framework;
using Mono.Cecil;
using System.IO;
using System.Xml;
using Mono.Cecil.Pdb;
using NDesk.Options;

namespace DependencyParser
{
    public class Program
    {
        private static readonly List<string> Parsed = new List<string>();

        private static readonly List<string> ToParse = new List<string>();

		private static readonly Lcom4Analyzer lcom4Analyzer = new Lcom4Analyzer();

		private static readonly ResponseForClassAnalyzer rfcAnalyzer = new ResponseForClassAnalyzer();

		private static readonly DethOfInheritanceTreeAnalyzer ditAnalyzer = new DethOfInheritanceTreeAnalyzer();

		private static readonly DependencyAnalyzer dependencyAnalyzer = new DependencyAnalyzer();

		private static bool designAnalysis = false;

        public static void Main(string[] args)
        {
            bool showHelp = false;
            string assemblyName =   "ASSEMBLY.DLL";
            string outputPath = "output.xml";
        	IEnumerable<string> ignorableFieldNames = null;
            
			var p = new OptionSet() 
            {
                { "a|assembly=", "the name of the assembly to scan", v => assemblyName = v },
                { "o|output=", "the path to the output XML", v => outputPath = v },
                { "h|help",  "show this message and exit", v => showHelp = v != null },
				{ "d|design",  "flag that enables design analysis", d => designAnalysis = d != null },
				{ "i|ignorable_fields=",  "When design analysis is enabled, comma list of names of fields that should not be taken in account for LCOM4 analysis", list => ignorableFieldNames = list.Split(',') }
            };

        	try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("DependencyParser: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `greet --help' for more information.");
                return;
            }

			if (showHelp)
			{
				Console.WriteLine("Use -a=[ASSSEMBLY] where ASSEMBLY is an assembly file");
				Console.WriteLine("and -o=[OUTPUT] where OUTPUT is the xml report file that will be generated");
				return;
			}

			if (ignorableFieldNames!=null)
			{
				lcom4Analyzer.IgnorableFieldNames = ignorableFieldNames;
			}


            var targetFolder = Path.GetDirectoryName(assemblyName);
            using (var stream = new FileStream(outputPath, FileMode.Create))
            {
                using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
                {
					var definition = AssemblyDefinition.ReadAssembly(assemblyName, new ReaderParameters { AssemblyResolver = AssemblyResolver.Resolver });

                    writer.Formatting = Formatting.Indented;
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Dependencies");
                    writer.WriteAttributeString("name", definition.MainModule.Assembly.Name.Name);
                    writer.WriteAttributeString("version", definition.MainModule.Assembly.Name.Version.ToString());

                    Analysis(writer, definition.MainModule, assemblyName, true);

                    while (ToParse.Count > 0)
                    {
                        definition = null;
                        var fullName = ToParse.First();
                        var assemblyNameDef = AssemblyNameReference.Parse(fullName);
                        var name = assemblyNameDef.Name;

                        // find that file
                        var dllFile = new FileInfo(Path.Combine(targetFolder, name + ".dll"));
                        var exeFile = new FileInfo(Path.Combine(targetFolder, name + ".exe"));
                        FileInfo targetFile = null;

                        if (dllFile.Exists)
                        {
                            targetFile = dllFile;
                        }
                        else if (exeFile.Exists)
                        {
                            targetFile = exeFile;
                        }

                        if (targetFile != null)
                        {
                            definition = AssemblyDefinition.ReadAssembly(targetFile.FullName);

                            if (definition != null)
                            {
                                if (!definition.FullName.Equals(fullName))
                                {
                                    Console.WriteLine("The existing file {0} doesn't match the fullName {1}, skip it", name, fullName);
                                    ToParse.Remove(fullName);
                                    Parsed.Add(fullName);
                                }
                                else
                                {
                                    Analysis(writer, definition.MainModule, targetFile.FullName, false);
                                }
                            }
                        }
                        else
                        {
                            // how to do for the GAC?
                            //definition = AssemblyDefinition.ReadAssembly()
                            Console.WriteLine("Skip {0}... maybe in the GAC?", name);
                            ToParse.Remove(fullName);
                            Parsed.Add(fullName);
                        }
                    }

					writer.WriteEndElement();

                    writer.WriteEndDocument();
                }
            }
        }

        static void Analysis(XmlTextWriter writer, ModuleDefinition module, string fullPath, bool withTypes)
        {
            try
            {
                module.ReadSymbols();

                var provider = new PdbReaderProvider();
                var reader = provider.GetSymbolReader(module, fullPath);
            }
            catch (FileNotFoundException)
            {

            }

            Console.WriteLine("Parsing {0}", module.Name);
            writer.WriteStartElement("Assembly");
            writer.WriteAttributeString("name", module.Assembly.Name.Name);
            writer.WriteAttributeString("version", module.Assembly.Name.Version.ToString());
            writer.WriteStartElement("References");
            foreach (var item in module.AssemblyReferences)
            {
                writer.WriteStartElement("Reference");
                writer.WriteAttributeString("name", item.Name);
                writer.WriteAttributeString("fullName", item.FullName);
                writer.WriteAttributeString("version", item.Version.ToString());
                writer.WriteEndElement();

                if (!Parsed.Contains(item.FullName) && !ToParse.Contains(item.FullName))
                {
                    ToParse.Add(item.FullName);
                }
            }
            writer.WriteEndElement();

            if (withTypes)
            {
                writer.WriteStartElement("TypeReferences");
                foreach (var t in module.Types)
                {
                    ParseType(writer, t);
                }

                writer.WriteEndElement();

				if (designAnalysis)
				{
					GenerateTypeDesignMeasures(writer, module);	
				}
            }

            writer.WriteEndElement();

            if (ToParse.Contains(module.Assembly.Name.FullName))
            {
                ToParse.Remove(module.Assembly.Name.FullName);
            }

            Parsed.Add(module.Assembly.Name.FullName);
        }

		public static void GenerateTypeDesignMeasures(XmlTextWriter writer, ModuleDefinition module)
		{
			writer.WriteStartElement("Design");
			var sourceRegistry = new SourceRegistry(module);
			
			// first generate and write measures for types colocated in the same files
			GenerateMultiTypeDesignMeasures(writer, module, sourceRegistry);

			// then deal with type with source locations not treated before
			foreach (var t in module.Types)
			{
				var path = t.GetSourcePath();
				if (path!=null && !sourceRegistry.IsMultiTypeFile(path))
				{
					DesignMeasures measures = GenerateTypeMeasures(t);
					measures.Write(writer);
				}
			}

			// and at last deal with types without source location
			foreach (var t in module.Types) {
				var path = t.GetSourcePath();
				if (path == null) {
					var measures = GenerateTypeMeasures(t);
					measures.Write(writer);
				}
			}

			writer.WriteEndElement();
		}

    	private static DesignMeasures GenerateTypeMeasures(TypeDefinition t)
		{
			return new DesignMeasures {
				Type = t,
				Lcom4Blocks = lcom4Analyzer.FindLcomBlocks(t),
				ResponseForClass = rfcAnalyzer.ComputeRFC(t),
				DethOfInheritance = ditAnalyzer.ComputeDIT(t)
			};
		}

    	private static void GenerateMultiTypeDesignMeasures(XmlTextWriter writer, ModuleDefinition module, SourceRegistry sourceRegistry)
		{
			var multiTypeMeasures = new Dictionary<string, DesignMeasures>();
			foreach (var t in module.Types) {

				var path = t.GetSourcePath();

				if (sourceRegistry.IsMultiTypeFile(path)) {
					var measures = GenerateTypeMeasures(t);
					if (multiTypeMeasures.ContainsKey(path)) {
						multiTypeMeasures[path] = multiTypeMeasures[path].Merge(measures);
					} else {
						multiTypeMeasures[path] = measures;
					}
				}
			}
			foreach (var multiTypeMeasure in multiTypeMeasures.Values) {
				multiTypeMeasure.Write(writer);
			}
		}

		public static void ParseType(XmlTextWriter writer, TypeDefinition t)
		{
			var dependencies = dependencyAnalyzer.FindTypeDependencies(t);
			if (dependencies != null && dependencies.Count() > 0) {
				writer.WriteStartElement("From");
				writer.WriteAttributeString("fullname", t.FullName);

				foreach (var to in dependencies) {
					writer.WriteStartElement("To");
					writer.WriteAttributeString("fullname", to.FullName);
					if (to.Scope is ModuleDefinition) {
						writer.WriteAttributeString("assemblyname", ((ModuleDefinition)to.Scope).Assembly.Name.Name);
						writer.WriteAttributeString("assemblyversion", ((ModuleDefinition)to.Scope).Assembly.Name.Version.ToString());
					} else if (to.Scope is AssemblyNameReference) {
						writer.WriteAttributeString("assemblyname", ((AssemblyNameReference)to.Scope).Name);
						writer.WriteAttributeString("assemblyversion", ((AssemblyNameReference)to.Scope).Version.ToString());
					}

					writer.WriteEndElement();
				}

				writer.WriteEndElement();
			}
		}

    }
}
