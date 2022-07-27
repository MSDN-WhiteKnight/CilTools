/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CilTools.Syntax;
using CilView.Core;
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

                yield return TextParagraph.Text("Print disassembled CIL code of the specified type or method:");
                yield return TextParagraph.Code("    " + exeName +
                    " view [--nocolor] <assembly path> <type full name> [<method name>]");
                yield return TextParagraph.Text("Print contents of the specified CIL source file (*.il):");
                yield return TextParagraph.Code("    " + exeName + " view [--nocolor] <source file path>");
                yield return TextParagraph.Text(string.Empty);
                yield return TextParagraph.Text("[--nocolor] - Disable syntax highlighting");
            }
        }

        static void PrintSourceDocument(string content, bool noColor, TextWriter target)
        {
            if (noColor)
            {
                target.WriteLine(content);
                return;
            }

            SyntaxNode[] nodes = SyntaxReader.ReadAllNodes(content);

            for (int i = 0; i < nodes.Length; i++)
            {
                Disassembler.PrintNode(nodes[i], noColor, target);
            }
        }

        public override int Execute(string[] args)
        {
            string filepath;
            string type;
            string method;
            bool noColor = false;

            if (args.Length < 2)
            {
                Console.WriteLine("Error: not enough arguments for 'view' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            int pos = 1;

            if (CLI.TryReadExpectedParameter(args, pos, "--nocolor"))
            {
                noColor = true;
                pos++;
            }

            //read path for assembly or IL source file
            filepath = CLI.ReadCommandParameter(args, pos);
            pos++;

            if (string.IsNullOrEmpty(filepath))
            {
                Console.WriteLine("Error: File path is not provided for the 'view' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            if (FileUtils.HasCilSourceExtension(filepath) ||
                (args.Length < 4 && !FileUtils.HasPeFileExtension(filepath)))
            {
                //view IL source file

                try
                {
                    string content = File.ReadAllText(filepath);
                    string title = Path.GetFileName(filepath);
                    Console.WriteLine("IL source file: " + title);
                    Console.WriteLine();
                    PrintSourceDocument(content, noColor, Console.Out);
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error:");
                    Console.WriteLine(ex.ToString());
                    return 1;
                }
            }

            //read type and method name from arguments
            type = CLI.ReadCommandParameter(args, pos);
            pos++;

            if (type == null)
            {
                Console.WriteLine("Error: Type name is not provided for the 'view' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            method = CLI.ReadCommandParameter(args, pos);

            Console.WriteLine("Assembly: " + filepath);

            if (string.IsNullOrEmpty(method))
            {
                //view type
                Console.WriteLine();
                return Disassembler.DisassembleType(filepath, type, false, noColor, Console.Out);
            }

            //view method
            Console.WriteLine("{0}.{1}", type, method);
            Console.WriteLine();
            return Disassembler.DisassembleMethod(filepath, type, method, noColor, Console.Out);
        }
    }
}
