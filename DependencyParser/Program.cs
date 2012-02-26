using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using System.IO;
using System.Xml;
using Mono.Cecil.Pdb;
using Mono.Cecil.Rocks;
using NDesk.Options;

namespace DependencyParser
{
    class Program
    {
        private static readonly List<string> parsed = new List<string>();

        private static readonly List<string> toParse = new List<string>();

        private static readonly Dictionary<string, IList<string>> dependencies = new Dictionary<string, IList<string>>();

        static void Main(string[] args)
        {
            bool show_help = false;
            string assemblyName =   @"C:\Users\mathias\Documents\visual studio 2010\Projects\chiffrage\Chiffrage\bin\Debug\Chiffrage.exe";
            string outputPath = "output.xml";
            
            var p = new OptionSet() 
            {
                { "a|assembly=", "the name of the assembly to scan", v => assemblyName = v },
                { "o|output=", "the path to the output XML", v => outputPath = v },
                { "h|help",  "show this message and exit", v => show_help = v != null },
            };

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("DependencyParser: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `greet --help' for more information.");
                return;
            }

            var targetFolder = Path.GetDirectoryName(assemblyName);
            using (var stream = new FileStream(outputPath, FileMode.Create))
            {
                using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
                {
                    var definition = AssemblyDefinition.ReadAssembly(assemblyName);

                    writer.Formatting = Formatting.Indented;
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Dependencies");
                    writer.WriteAttributeString("name", definition.MainModule.Assembly.Name.Name);
                    writer.WriteAttributeString("version", definition.MainModule.Assembly.Name.Version.ToString());

                    Analysis(writer, definition.MainModule, assemblyName);

                    while (toParse.Count > 0)
                    {
                        definition = null;
                        var fullName = toParse.First();
                        var assemblyNameDef = AssemblyNameDefinition.Parse(fullName);
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
                                    toParse.Remove(fullName);
                                    parsed.Add(fullName);
                                }
                                else
                                {
                                    Analysis(writer, definition.MainModule, targetFile.FullName);
                                }
                            }
                        }
                        else
                        {
                            // how to do for the GAC?
                            //definition = AssemblyDefinition.ReadAssembly()
                            Console.WriteLine("Skip {0}... maybe in the GAC?", name);
                            toParse.Remove(fullName);
                            parsed.Add(fullName);
                        }
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
        }

        static void Analysis(XmlTextWriter writer, ModuleDefinition module, string fullPath)
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

                if (!parsed.Contains(item.FullName) && !toParse.Contains(item.FullName))
                {
                    toParse.Add(item.FullName);
                }

                /*foreach (var t in module.Types)
                {
                    foreach (var c in t.CustomAttributes)
                    {
                        AddDependency(t, c.AttributeType);
                    }

                    if (t.BaseType != null)
                    {
                        AddDependency(t, t.BaseType);
                    }

                    foreach (var i in t.Interfaces)
                    {
                        AddDependency(t, i);
                    }

                    foreach (var e in t.Events)
                    {
                        AddDependency(t, e.EventType);
                    }

                    foreach (var f in t.Fields)
                    {
                        AddDependency(t, f.FieldType);
                    }

                    foreach (var p in t.Properties)
                    {
                        AddDependency(t, p.PropertyType);
                    }

                    foreach (var m in t.Methods)
                    {
                        AddDependency(t, m.ReturnType);

                        foreach (var p in m.Parameters)
                        {
                            AddDependency(t, p.ParameterType);
                        }

                        if (m.Body != null)
                        {
                            //m.Body.Instructions[0].SequencePoint.Document

                            foreach (var v in m.Body.Variables)
                            {
                                AddDependency(t, v.VariableType);
                            }

                            foreach (var e in m.Body.ExceptionHandlers)
                            {
                                if (e.CatchType != null)
                                {
                                    AddDependency(t, e.CatchType);
                                }
                            }
                        }
                    }
                }*/
            }
            writer.WriteEndElement();
            writer.WriteEndElement();

            if (toParse.Contains(module.Assembly.Name.FullName))
            {
                toParse.Remove(module.Assembly.Name.FullName);
            }

            parsed.Add(module.Assembly.Name.FullName);
        }

        public static void AddDependency(TypeDefinition from, TypeReference to)
        {
            IList<string> toList;
            if (!dependencies.TryGetValue(from.FullName, out toList))
            {
                toList = new List<string>();
                dependencies.Add(from.FullName, toList);
            }

            if (!to.FullName.StartsWith("System") && !to.FullName.StartsWith("Microsoft"))
            {
                if (!to.IsGenericParameter)
                {
                    if (!toList.Contains(to.FullName))
                    {
                        toList.Add(to.FullName);
                    }
                }
            }
        }
    }
}
