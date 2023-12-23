/* CIL Tools
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Metadata;
using CilTools.Syntax;
using CilTools.Syntax.Tokens;
using CilTools.Visualization;
using CilView.Common;
using CilView.Core;
using CilView.Core.Documentation;
using CilView.Core.Syntax;

namespace CilTools.CommandLine
{
    class ViewCommand : Command
    {
        const string HtmlFileName = "CilTools.html";

        public override string Name 
        { 
            get { return "view"; } 
        }

        public override string Description 
        { 
            get { return "Print CIL code of types or methods or the content of CIL source files"; } 
        }

        public override IEnumerable<TextParagraph> UsageDocumentation
        {
            get
            {
                string exeName = typeof(Program).Assembly.GetName().Name;

                yield return TextParagraph.Text("Print disassembled CIL code of the specified assembly, type or method:");
                yield return TextParagraph.Code("    " + exeName +
                    " view [--nocolor] [--html] <assembly path> [<type full name>] [<method name>]");
                yield return TextParagraph.Text("Print contents of the specified CIL source file (*.il):");
                yield return TextParagraph.Code("    " + exeName + " view [--nocolor] [--html] <source file path>");
                yield return TextParagraph.Text(string.Empty);
                yield return TextParagraph.Text("[--nocolor] - Disable syntax highlighting");
                yield return TextParagraph.Text("[--html] - Output format is HTML");
            }
        }
        
        static FileStream TryCreateFile(string name)
        {
            FileStream fs;

            try
            {
                //try in current directory
                fs = new FileStream(name, FileMode.Create, FileAccess.Write);
            }
            catch (IOException)
            {
                //try in temp directory
                string path = Path.Combine(Path.GetTempPath(), name);
                fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            }

            return fs;
        }

        static void OpenInBrowser(string filePath)
        {
            Console.WriteLine("Trying to open " + filePath + " in browser...");
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = filePath;
            psi.UseShellExecute = true;
            Process pr = Process.Start(psi);

            if (pr != null) pr.Dispose();
        }
        
        static int View(string filepath, string typeName, bool html, bool noColor)
        {
            AssemblyReader reader = new AssemblyReader();
            FileStream fs = null;
            TextWriter target;

            try
            {
                Assembly ass;
                IEnumerable<SyntaxNode> nodes;

                // Build syntax nodes
                if (FileUtils.HasCilSourceExtension(filepath) ||
                    (string.IsNullOrEmpty(typeName) && !FileUtils.HasPeFileExtension(filepath))) //source file
                {
                    string content = File.ReadAllText(filepath);
                    string title = Path.GetFileName(filepath);
                    Console.WriteLine("IL source file: " + title);
                    nodes = SyntaxReader.ReadAllNodes(content);
                }
                else if (string.IsNullOrEmpty(typeName)) //assembly manifest
                {
                    Console.WriteLine("Assembly: " + filepath);
                    ass = reader.LoadFrom(filepath);
                    nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);
                }
                else //type
                {
                    Console.WriteLine("Assembly: " + filepath);
                    ass = reader.LoadFrom(filepath);
                    Type t = ass.GetType(typeName);

                    if (t == null)
                    {
                        Console.WriteLine("Error: Type {0} not found in assembly {1}", typeName, filepath);
                        return 1;
                    }

                    nodes = SyntaxNode.GetTypeDefSyntax(t, full: false, new DisassemblerParams());
                }

                // Determine output target
                if (html)
                {
                    fs = TryCreateFile(HtmlFileName); //create output HTML file

                    if (fs == null)
                    {
                        Console.WriteLine("Error: failed to create output HTML file.");
                        return 1;
                    }

                    target = new StreamWriter(fs);
                    SyntaxWriter.WriteDocumentStart(target);
                }
                else target = Console.Out;

                // Visualize syntax nodes
                SyntaxVisualizer vis;
                VisualizationOptions options = new VisualizationOptions();

                if (html) vis = new HtmlVisualizer();
                else if (noColor) vis = SyntaxVisualizer.Create(OutputFormat.Plaintext);
                else vis = SyntaxVisualizer.Create(OutputFormat.ConsoleText);
                
                if (noColor) options.EnableSyntaxHighlighting = false;

                Console.WriteLine();
                vis.RenderNodes(nodes, options, target);

                if (html)
                {
                    SyntaxWriter.WriteDocumentEnd(target);
                    
                    // Open output file in browser
                    OpenInBrowser(fs.Name);

                    // Close target file stream
                    fs.Dispose();
                    fs = null;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(ex.ToString());
                return 1;
            }
            finally
            {
                reader.Dispose();
                if (fs != null) fs.Dispose();
            }
        }

        static void PrintMethod(MethodBase method, SyntaxVisualizer vis, VisualizationOptions options, TextWriter target)
        {
            CilGraph graph = CilGraph.Create(method);
            SyntaxNode root = graph.ToSyntaxTree();
            vis.RenderNodes(new SyntaxNode[] { root }, options, target);
        }

        static int ViewMethod(string filepath, string typeName, string methodName, bool html, bool noColor)
        {
            AssemblyReader reader = new AssemblyReader();
            FileStream fs = null;
            TextWriter target;

            try
            {
                // Find method group to visualize
                Assembly ass = reader.LoadFrom(filepath);
                Type t = ass.GetType(typeName);

                if (t == null)
                {
                    Console.WriteLine("Error: Type {0} not found in assembly {1}", typeName, filepath);
                    return 1;
                }

                MemberInfo[] methods = t.GetMembers(Utils.AllMembers);

                MethodBase[] selectedMethods = methods.OfType<MethodBase>().Where(
                        (x) => Utils.StringEquals(x.Name, methodName)
                    ).ToArray();

                if (selectedMethods.Length == 0)
                {
                    Console.WriteLine("Error: Type {0} does not declare methods with the specified name", typeName);
                    return 1;
                }
                
                // Determine output target
                if (html)
                {
                    fs = TryCreateFile(HtmlFileName); //create output HTML file

                    if (fs == null)
                    {
                        Console.WriteLine("Error: failed to create output HTML file.");
                        return 1;
                    }

                    target = new StreamWriter(fs);
                    SyntaxWriter.WriteDocumentStart(target);
                }
                else target = Console.Out;
                
                SyntaxVisualizer vis;
                VisualizationOptions options = new VisualizationOptions();

                if (html) vis = new HtmlVisualizer();
                else if (noColor) vis = SyntaxVisualizer.Create(OutputFormat.Plaintext);
                else vis = SyntaxVisualizer.Create(OutputFormat.ConsoleText);

                if (noColor) options.EnableSyntaxHighlighting = false;

                // Visualize selected methods
                for (int i = 0; i < selectedMethods.Length; i++)
                {
                    PrintMethod(selectedMethods[i], vis, options, target);
                    target.WriteLine();
                }

                if (html)
                {
                    SyntaxWriter.WriteDocumentEnd(target);

                    // Open output file in browser
                    OpenInBrowser(fs.Name);

                    // Close target file stream
                    fs.Dispose();
                    fs = null;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(ex.ToString());
                return 1;
            }
            finally
            {
                reader.Dispose();
                if (fs != null) fs.Dispose();
            }
        }

        public override int Execute(string[] args)
        {
            string filepath = string.Empty;
            string type = string.Empty;
            string method = string.Empty;
            bool noColor = false;
            bool html = false;

            if (args.Length < 2)
            {
                Console.WriteLine("Error: not enough arguments for 'view' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            // Parse command line arguments
            NamedArgumentDefinition[] defs = new NamedArgumentDefinition[]
            {
                new NamedArgumentDefinition("--nocolor", false, "Disable syntax highlighting"),
                new NamedArgumentDefinition("--html", false, "Output format is HTML")
            };

            CommandLineArgs cla = new CommandLineArgs(args, defs);

            if (cla.HasNamedArgument("--nocolor")) noColor = true;

            if (cla.HasNamedArgument("--html")) html = true;

            if (cla.PositionalArgumentsCount > 1)
            {
                //read path for assembly or IL source file
                filepath = cla.GetPositionalArgument(1);
            }

            if (string.IsNullOrEmpty(filepath))
            {
                Console.WriteLine("Error: File path is not provided for the 'view' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            //read type and method name from arguments
            if (cla.PositionalArgumentsCount > 2) type = cla.GetPositionalArgument(2);

            if (cla.PositionalArgumentsCount > 3) method = cla.GetPositionalArgument(3);
            
            if (!File.Exists(filepath) && FileUtils.IsFileNameWithoutDirectory(filepath) &&
                filepath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                // If full path is not specified, try to find path for BCL assembly of the current runtime
                string bclPath = FileUtils.GetBclAssemblyPath(filepath);

                if (File.Exists(bclPath)) filepath = bclPath;
            }
            
            if (string.IsNullOrEmpty(method) || FileUtils.HasCilSourceExtension(filepath))
            {
                //source file, type or assembly manifest
                return View(filepath, type, html, noColor);
            }

            //method
            Console.WriteLine("Assembly: " + filepath);
            Console.WriteLine("{0}.{1}", type, method);
            Console.WriteLine();
            
            return ViewMethod(filepath, type, method, html, noColor);
        }
    }
}
