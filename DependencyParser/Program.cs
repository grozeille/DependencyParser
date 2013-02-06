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

        public static void Main(string[] args)
        {
            bool showHelp = false;
            string assemblyName =   "ASSEMBLY.DLL";
            string outputPath = "output.xml";
            
            var p = new OptionSet() 
            {
                { "a|assembly=", "the name of the assembly to scan", v => assemblyName = v },
                { "o|output=", "the path to the output XML", v => outputPath = v },
                { "h|help",  "show this message and exit", v => showHelp = v != null },
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
				
				writer.WriteStartElement("Design");
				foreach (var t in module.Types) {
					GenerateTypeDesignMeasures(writer, t);
				}
				writer.WriteEndElement();
				
            }

            writer.WriteEndElement();

            if (ToParse.Contains(module.Assembly.Name.FullName))
            {
                ToParse.Remove(module.Assembly.Name.FullName);
            }

            Parsed.Add(module.Assembly.Name.FullName);
        }

		public static void GenerateTypeDesignMeasures(XmlTextWriter writer, TypeDefinition t)
		{
			var designWriter = new DesignMeasuresWriter() { Xml = writer, Type = t};
			designWriter.Lcom4Blocks = lcom4Analyzer.FindLcomBlocks(t);
			designWriter.ResponseForClass = rfcAnalyzer.ComputeRFC(t);
			designWriter.DethOfInheritance = ditAnalyzer.ComputeDIT(t);

			designWriter.Write();
		}

        public static void ParseType(XmlTextWriter writer, TypeDefinition t)
        {
            // ignore generated types
            if (t.DeclaringType == null && t.Namespace.Equals(string.Empty))
            {
                return;
            }

            if (t.Name.StartsWith("<>"))
            {
                return;
            }

            foreach (var n in t.NestedTypes)
            {
                ParseType(writer, n);
            }

            Dictionary<string, IList<string>> cache = new Dictionary<string, IList<string>>();
            writer.WriteStartElement("From");
            writer.WriteAttributeString("fullname", t.FullName);

            foreach (var c in t.CustomAttributes)
            {
                AddDependency(writer, cache, t, c.AttributeType);
            }

            if (t.BaseType != null)
            {
                AddDependency(writer, cache, t, t.BaseType);
            }

            foreach (var i in t.Interfaces)
            {
                AddDependency(writer, cache, t, i);
            }

            foreach (var e in t.Events)
            {
                AddDependency(writer, cache, t, e.EventType);
            }

            foreach (var f in t.Fields)
            {
                AddDependency(writer, cache, t, f.FieldType);
            }

            foreach (var p in t.Properties)
            {
                AddDependency(writer, cache, t, p.PropertyType);
            }

            foreach (var m in t.Methods)
            {
                AddDependency(writer, cache, t, m.ReturnType);

                foreach (var p in m.Parameters)
                {
                    AddDependency(writer, cache, t, p.ParameterType);
                }

                if (m.Body != null)
                {
                    //m.Body.Instructions[0].SequencePoint.Document

                    foreach (var v in m.Body.Variables)
                    {
                        AddDependency(writer, cache, t, v.VariableType);
                    }

                    foreach (var e in m.Body.ExceptionHandlers)
                    {
                        if (e.CatchType != null)
                        {
                            AddDependency(writer, cache, t, e.CatchType);
                        }
                    }
                }
            }

            writer.WriteEndElement();
        }

        public static void AddDependency(XmlTextWriter writer, IDictionary<string, IList<string>> cache, TypeDefinition from, TypeReference to)
        {
            if (from.FullName.Equals(to.FullName))
            {
                return;
            }

            // ignore generic parameters
            if (to.IsGenericParameter)
            {
                return;
            }

            // ignore generated types, without namespace
            if (to.Namespace.Equals(string.Empty))
            {
                return;
            }

            if (to.IsArray)
            {
                to = to.GetElementType();
            }

            if (to.IsGenericInstance)
            {
                var generic = (GenericInstanceType)to;
                foreach (var a in generic.GenericArguments)
                {
                    AddDependency(writer, cache, from, a);
                }
                to = to.GetElementType();
            }

            // ignore types from .Net framework
            if (to.Scope.Name.Equals("mscorlib") || to.Scope.Name.StartsWith("System") || to.Scope.Name.StartsWith("Microsoft"))
            {
                return;
            }

            IList<string> toList;
            if (!cache.TryGetValue(from.FullName, out toList))
            {
                toList = new List<string>();
                cache.Add(from.FullName, toList);
            }

            if (toList.Contains(to.FullName))
            {
                return;
            }

            
            writer.WriteStartElement("To");
            writer.WriteAttributeString("fullname", to.FullName);
            if (to.Scope is ModuleDefinition)
            {
                writer.WriteAttributeString("assemblyname", ((ModuleDefinition)to.Scope).Assembly.Name.Name);
                writer.WriteAttributeString("assemblyversion", ((ModuleDefinition)to.Scope).Assembly.Name.Version.ToString());
            }
            else if(to.Scope is AssemblyNameReference)
            {
                writer.WriteAttributeString("assemblyname", ((AssemblyNameReference)to.Scope).Name);
                writer.WriteAttributeString("assemblyversion", ((AssemblyNameReference)to.Scope).Version.ToString());
            }

            writer.WriteEndElement();

            toList.Add(to.FullName);
        }
    }
}
