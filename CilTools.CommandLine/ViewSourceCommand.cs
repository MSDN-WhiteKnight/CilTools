/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
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
using CilTools.Syntax;
using CilView.Common;
using CilView.Core.Documentation;
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
                    " view-source [--nocolor] <assembly path> <type full name> <method name>");
                yield return TextParagraph.Text(string.Empty);
                yield return TextParagraph.Text("[--nocolor] - Disable syntax highlighting");
                yield return TextParagraph.Text(string.Empty);

                yield return TextParagraph.Text("For methods with body, this command can print source code " +
                    "based on symbols, if they are available. For methods without body, the command prints a " +
                    "disassembled source code.");
            }
        }

        static void PrintNode(SyntaxNode node, bool noColor, TextWriter target)
        {
            //recursively prints source fragment's syntax tree to console
            SyntaxNode[] children = node.GetChildNodes();

            if (children.Length == 0)
            {
                //if it a leaf node, print its content to console

                if (noColor)
                {
                    //no syntax highlighting
                    node.ToText(target);
                    return;
                }

                //highlight syntax elements
                ConsoleColor originalColor = Console.ForegroundColor;
                
                if(node is SourceToken){
                    SourceToken token = (SourceToken)node;
                    
                    switch (token.Kind)
                    {
                        case TokenKind.Keyword: Console.ForegroundColor = ConsoleColor.Magenta; break;
                        case TokenKind.TypeName: Console.ForegroundColor = ConsoleColor.Cyan; break;
                        case TokenKind.DoubleQuotLiteral: Console.ForegroundColor = ConsoleColor.Red; break;
                        case TokenKind.SingleQuotLiteral: Console.ForegroundColor = ConsoleColor.Red; break;
                        case TokenKind.Comment: Console.ForegroundColor = ConsoleColor.Green; break;
                        case TokenKind.MultilineComment: Console.ForegroundColor = ConsoleColor.Green; break;
                    }
                }

                node.ToText(target);

                //restore console color to default value
                Console.ForegroundColor = originalColor;
            }
            else
            {
                //if the node has child nodes, process them

                for (int i = 0; i < children.Length; i++)
                {
                    PrintNode(children[i], noColor, target);
                }
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

        static int PrintMethodSource(MethodBase mb, bool noColor)
        {
            if (Utils.IsMethodWithoutBody(mb))
            {
                //method without IL body has no sequence points in PDB, just use decompiler
                IEnumerable<SourceToken> decompiled = Decompiler.DecompileMethodSignature(".cs", mb);
                Console.WriteLine("Source code from: Decompiler");
                Console.WriteLine();

                foreach (SourceToken token in decompiled)
                {
                    PrintNode(token, noColor, Console.Out);
                }

                Console.WriteLine();
                return 0;
            }

            //from PDB
            PdbCodeProvider provider = PdbCodeProvider.Instance;
            SourceDocument doc = provider.GetSourceCodeDocuments(mb).FirstOrDefault();

            if (doc == null)
            {
                Console.WriteLine("Error: Line info not found for this method.");
                return 1;
            }

            if (string.IsNullOrEmpty(doc.Text))
            {
                //Local sources not available
                string sourceLinkStr = doc.SourceLinkMap;

                if (string.IsNullOrEmpty(sourceLinkStr))
                {
                    Console.WriteLine("Error: Source file " + doc.FilePath + " is not found or empty.");
                    return 1;
                }
                else
                {
                    //Source Link stub implementation
                    Console.WriteLine("The source code is located on the remote server:");
                    Console.WriteLine(sourceLinkStr);
                    Console.WriteLine("File path: " + doc.FilePath);
                    return 1;
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

            SourceToken[] bodyTokens = SourceTokenReader.ReadAllTokens(sb.ToString(), SourceCodeUtils.GetTokenDefinitions(ext),
                SourceCodeUtils.CreateClassifier(ext));

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

            for (int i = 0; i < tokens.Count; i++)
            {
                PrintNode(tokens[i], noColor, Console.Out);
            }

            Console.WriteLine();
            Console.WriteLine(caption);
            Console.WriteLine();
            return 0;
        }

        public override int Execute(string[] args)
        {
            string filepath;
            string type;
            string method;
            bool noColor = false;

            if (args.Length < 4)
            {
                Console.WriteLine("Error: not enough arguments for 'view-source' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            int pos = 1;

            if (CLI.TryReadExpectedParameter(args, pos, "--nocolor"))
            {
                noColor = true;
                pos++;
            }

            //read path for assembly
            filepath = CLI.ReadCommandParameter(args, pos);
            pos++;

            if (string.IsNullOrEmpty(filepath))
            {
                Console.WriteLine("Error: File path is not provided for the 'view-source' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            //read type and method name from arguments
            type = CLI.ReadCommandParameter(args, pos);
            pos++;

            if (type == null)
            {
                Console.WriteLine("Error: Type name is not provided for the 'view-source' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            method = CLI.ReadCommandParameter(args, pos);

            if (method == null)
            {
                Console.WriteLine("Error: Method name is not provided for the 'view-source' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            //view method source
            Console.WriteLine("Assembly: " + filepath);
            Console.WriteLine("Type: " + type);
            Console.WriteLine();

            AssemblyReader reader = new AssemblyReader();

            try
            {
                //read methods from assembly
                Assembly ass = reader.LoadFrom(filepath);
                Type t = ass.GetType(type);

                if (t == null)
                {
                    Console.WriteLine("Error: Type {0} not found in assembly {1}", type, filepath);
                    return 1;
                }

                MemberInfo[] methods = t.GetMembers(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
                    );

                MethodBase[] selectedMethods = methods.OfType<MethodBase>().Where((x) => { return x.Name == method; }).ToArray();

                if (selectedMethods.Length == 0)
                {
                    Console.WriteLine("Error: Type {0} does not declare methods with the specified name", type);
                    return 1;
                }

                //print sources for matching methods
                int retCode = 0;

                for (int i = 0; i < selectedMethods.Length; i++)
                {
                    Console.WriteLine(MethodToString(selectedMethods[i]));

                    int res = PrintMethodSource(selectedMethods[i], noColor);

                    if (res != 0) retCode = res;
                }

                return retCode;
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
            }
        }
    }
}
