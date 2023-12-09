/* CIL Tools
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CilTools.Syntax;
using CilTools.Syntax.Tokens;
using CilTools.Visualization;
using CilView.Core;
using CilView.Core.Documentation;
using CilView.Core.Syntax;

namespace CilTools.CommandLine
{
    class ViewCommand : Command
    {
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
                    " view [--nocolor] <assembly path> [<type full name>] [<method name>]");
                yield return TextParagraph.Text("Print contents of the specified CIL source file (*.il):");
                yield return TextParagraph.Code("    " + exeName + " view [--nocolor] <source file path>");
                yield return TextParagraph.Text(string.Empty);
                yield return TextParagraph.Text("[--nocolor] - Disable syntax highlighting");
            }
        }

        static void PrintSourceDocument(string content, bool noColor, bool html, TextWriter target)
        {
            if (noColor && !html)
            {
                target.WriteLine(content);
                return;
            }

            SyntaxNode[] nodes = SyntaxReader.ReadAllNodes(content);

            if (html)
            {
                HtmlVisualizer vis = new HtmlVisualizer();
                SyntaxWriter.WriteDocumentStart(target);
                vis.RenderSyntaxNodes(nodes, new VisualizationOptions(), target);
                SyntaxWriter.WriteDocumentEnd(target);
            }
            else
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    Visualizer.PrintNode(nodes[i], noColor, target);
                }
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

        static int ViewMethodAsHtml(string asspath, string type, string method)
        {
            try
            {
                // Create output file
                FileStream fs = TryCreateFile("CilTools.html");

                if (fs == null)
                {
                    Console.WriteLine("Error: failed to create output HTML file.");
                    return 1;
                }

                // Write HTML to output file
                using (fs)
                {
                    TextWriter wr = new StreamWriter(fs);
                    Visualizer.DisassembleMethod(asspath, type, method, true, wr);
                }

                // Open output file in browser
                OpenInBrowser(fs.Name);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(ex.ToString());
                return 1;
            }
        }

        static int ViewContentAsHtml(string content)
        {
            // Create output file
            FileStream fs = TryCreateFile("CilTools.html");

            if (fs == null)
            {
                Console.WriteLine("Error: failed to create output HTML file.");
                return 1;
            }

            // Write HTML to output file
            using (fs)
            {
                TextWriter wr = new StreamWriter(fs);
                PrintSourceDocument(content, noColor: false, html: true, wr);
            }

            // Open output file in browser
            OpenInBrowser(fs.Name);

            return 0;
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

            if (FileUtils.HasCilSourceExtension(filepath) ||
                (cla.PositionalArgumentsCount <= 2 && !FileUtils.HasPeFileExtension(filepath)))
            {
                // View IL source file

                try
                {
                    string content = File.ReadAllText(filepath);
                    string title = Path.GetFileName(filepath);
                    Console.WriteLine("IL source file: " + title);
                    Console.WriteLine();

                    if (html)
                    {
                        return ViewContentAsHtml(content);
                    }
                    else
                    {
                        PrintSourceDocument(content, noColor, false, Console.Out);
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error:");
                    Console.WriteLine(ex.ToString());
                    return 1;
                }
            }
            
            // View disassembled IL

            if (!File.Exists(filepath) && FileUtils.IsFileNameWithoutDirectory(filepath) &&
                filepath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                // If full path is not specified, try to find path for BCL assembly of the current runtime
                string bclPath = FileUtils.GetBclAssemblyPath(filepath);

                if (File.Exists(bclPath)) filepath = bclPath;
            }

            Console.WriteLine("Assembly: " + filepath);
            
            if (string.IsNullOrEmpty(type))
            {
                //view assembly manifest
                Console.WriteLine();
                return Visualizer.VisualizeAssembly(filepath, noColor, Console.Out);
            }
            
            if (string.IsNullOrEmpty(method))
            {
                //view type
                Console.WriteLine();
                return Visualizer.VisualizeType(filepath, type, false, noColor, Console.Out);
            }

            //view method
            Console.WriteLine("{0}.{1}", type, method);
            Console.WriteLine();

            if (html) return ViewMethodAsHtml(filepath, type, method);
            else return Visualizer.VisualizeMethod(filepath, type, method, noColor, Console.Out);
        }
    }
}
