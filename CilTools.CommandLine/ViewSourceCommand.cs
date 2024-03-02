/* CIL Tools
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.Metadata;
using CilTools.SourceCode;
using CilTools.SourceCode.Common;
using CilTools.Visualization;
using CilView.Common;
using CilView.Core.Documentation;
using CilView.Core.Syntax;
using CilView.SourceCode;

namespace CilTools.CommandLine
{
    class ViewSourceCommand : Command
    {
        public override string Name 
        { 
            get { return "view-source"; }
        }

        public override string Description 
        { 
            get { return "Print source code of the specified method"; } 
        }

        public override IEnumerable<TextParagraph> UsageDocumentation
        {
            get
            {
                string exeName = typeof(Program).Assembly.GetName().Name;

                yield return TextParagraph.Code("    " + exeName +
                    " view-source [--nocolor] [--html] <assembly path> <type full name> <method name>");
                yield return TextParagraph.Text(string.Empty);
                yield return TextParagraph.Text("[--nocolor] - Disable syntax highlighting");
                yield return TextParagraph.Text("[--html] - Output format is HTML");
                yield return TextParagraph.Text(string.Empty);

                yield return TextParagraph.Text("For methods with body, this command can print source code " +
                    "based on symbols, if they are available. For methods without body, the command prints a " +
                    "disassembled source code.");
            }
        }
        
        static string MethodToString(MethodBase m)
        {
            StringBuilder sb = new StringBuilder();
            ParameterInfo[] pars = m.GetParameters();
            sb.Append(m.Name);

            if (m.IsGenericMethod)
            {
                sb.Append('<');

                Type[] args = m.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) sb.Append(", ");

                    sb.Append(args[i].Name);
                }

                sb.Append('>');
            }

            sb.Append('(');

            for (int i = 0; i < pars.Length; i++)
            {
                if (i >= 1) sb.Append(", ");
                sb.Append(pars[i].ParameterType.Name);
            }

            sb.Append(')');
            return sb.ToString();
        }

        static void PrintMethodSource(MethodBase mb, SyntaxVisualizer vis, VisualizationOptions options, TextWriter target)
        {
            if (Utils.IsMethodWithoutBody(mb))
            {
                //method without IL body has no sequence points in PDB, just use decompiler
                IEnumerable<SourceToken> decompiled = Decompiler.DecompileMethodSignature(".cs", mb);
                Console.WriteLine("Source code from: Decompiler");
                Console.WriteLine();
                vis.RenderNodes(decompiled, options, target);
                Console.WriteLine();

                return;
            }

            //from PDB
            PdbCodeProvider provider = PdbCodeProvider.Instance;
            SourceDocument doc = provider.GetSourceCodeDocuments(mb).FirstOrDefault();

            if (doc == null)
            {
                Console.WriteLine("Error: Line info not found for this method.");
                return;
            }

            if (string.IsNullOrEmpty(doc.Text))
            {
                //Local sources not available
                string sourceLinkStr = doc.SourceLinkMap;

                if (string.IsNullOrEmpty(sourceLinkStr))
                {
                    Console.WriteLine("Error: Source file " + doc.FilePath + " is not found or empty.");
                    return;
                }
                else
                {
                    //Source Link stub implementation
                    Console.WriteLine("The source code is located on the remote server:");
                    Console.WriteLine(sourceLinkStr);
                    Console.WriteLine("File path: " + doc.FilePath);
                    return;
                }
            }

            string src = doc.Text;
            string ext = Path.GetExtension(doc.FilePath);
            StringBuilder sb;
            SourceToken[] sigTokens = new SourceToken[0];

            try
            {
                if (!Utils.IsConstructor(doc.Method) && !Utils.IsPropertyMethod(doc.Method))
                {
                    sigTokens = Decompiler.DecompileMethodSignature(ext, doc.Method).ToArray();
                }
            }
            catch (Exception ex)
            {
                //don't error out if we can't build good signature string
                Console.WriteLine("Warning: Failed to decompile method signature (" + ex.GetType().ToString() +
                    ": " + ex.Message + ")");
                string methodstr = MethodToString(doc.Method);
                sigTokens = new SourceToken[] { new SourceToken(methodstr, TokenKind.Unknown) };
            }

            //header
            sb = new StringBuilder();
            sb.Append("Source code from: ");
            sb.Append(doc.FilePath);
            sb.AppendFormat(", lines {0}-{1}", doc.LineStart, doc.LineEnd);
            string header = sb.ToString();

            //body
            sb = new StringBuilder(src.Length + 2);
            string srcDeindented = PdbUtils.Deindent(src);
            sb.Append(srcDeindented);

            if (Decompiler.IsCppExtension(ext))
            {
                //C++ PDB sequence points don't include the trailing brace for some reason
                if (!srcDeindented.EndsWith(Environment.NewLine, StringComparison.Ordinal)) sb.AppendLine();
                sb.Append('}');
            }

            SourceToken[] bodyTokens = SourceCodeUtils.ReadAllTokens(sb.ToString(), SourceCodeUtils.GetTokenDefinitions(ext),
                SourceCodeUtils.GetFactory(ext));

            List<SourceToken> tokens = new List<SourceToken>(sigTokens.Length + bodyTokens.Length + 1);
            tokens.AddRange(sigTokens);
            tokens.Add(new SourceToken(Environment.NewLine, TokenKind.Unknown));
            tokens.AddRange(bodyTokens);

            //caption
            sb = new StringBuilder();
            sb.Append("Symbols file: ");
            sb.Append(doc.SymbolsFile);
            sb.Append(" (");
            sb.Append(doc.SymbolsFileFormat);
            sb.Append(')');
            string caption = sb.ToString();

            //show source code
            Console.WriteLine(header);
            Console.WriteLine();
            vis.RenderNodes(tokens, options, target);
            Console.WriteLine();
            Console.WriteLine(caption);
            Console.WriteLine();
        }

        static int ViewMethodSource(string filepath, string typeName, string methodName, bool html, bool noColor)
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
                    fs = CLI.TryCreateFile(CLI.HtmlFileName); //create output HTML file

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
                    Console.WriteLine(MethodToString(selectedMethods[i]));
                    PrintMethodSource(selectedMethods[i], vis, options, target);
                    target.WriteLine();
                }

                if (html)
                {
                    SyntaxWriter.WriteDocumentEnd(target);

                    // Open output file in browser
                    CLI.OpenInBrowser(fs.Name);

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

            if (args.Length < 4)
            {
                Console.WriteLine("Error: not enough arguments for 'view-source' command.");
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
                //read path for assembly
                filepath = cla.GetPositionalArgument(1);
            }

            if (string.IsNullOrEmpty(filepath))
            {
                Console.WriteLine("Error: File path is not provided for the 'view-source' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            //read type and method name from arguments
            if (cla.PositionalArgumentsCount > 2) type = cla.GetPositionalArgument(2);

            if (string.IsNullOrEmpty(type))
            {
                Console.WriteLine("Error: Type name is not provided for the 'view-source' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            if (cla.PositionalArgumentsCount > 3) method = cla.GetPositionalArgument(3);

            if (string.IsNullOrEmpty(method))
            {
                Console.WriteLine("Error: Method name is not provided for the 'view-source' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }
            
            // View method source
            Console.WriteLine("Assembly: " + filepath);
            Console.WriteLine("Type: " + type);
            Console.WriteLine();

            return ViewMethodSource(filepath, type, method, html, noColor);
        }
    }
}
