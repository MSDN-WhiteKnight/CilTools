/* CIL Tools
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CilTools.Metadata;
using CilTools.Syntax;
using CilView.Core.Documentation;
using CilView.Core.Syntax;

namespace CilTools.CommandLine
{
    class DisasmCommand : Command
    {
        public override string Name 
        { 
            get { return "disasm"; } 
        }

        public override string Description 
        { 
            get { return "Write disassembled CIL code of the specified assembly, type or method into the file"; } 
        }

        public override IEnumerable<TextParagraph> UsageDocumentation
        {
            get
            {
                string exeName = typeof(Program).Assembly.GetName().Name;

                yield return TextParagraph.Code("    " + exeName +
                    " disasm [--output <output path>] [--html] <assembly path> [<type full name>] [<method name>]");
                yield return TextParagraph.Text("[--output <output path>] - Output file path");
                yield return TextParagraph.Text("[--html] - When specified, output format is HTML");
            }
        }

        static async Task<int> DisassembleAssembly(string asspath, bool html, TextWriter target)
        {
            AssemblyReader reader = new AssemblyReader();
            Assembly ass;
            int retCode;

            try
            {
                ass = reader.LoadFrom(asspath);

                if (html) await SyntaxWriter.DisassembleAsHtmlAsync(ass, new DisassemblerParams(), target);
                else await SyntaxWriter.DisassembleAsync(ass, new DisassemblerParams(), target);

                retCode = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(ex.ToString());
                retCode = 1;
            }
            finally
            {
                reader.Dispose();
            }

            return retCode;
        }

        static async Task<int> DisassembleType(string asspath, string type, bool html, TextWriter target)
        {
            AssemblyReader reader = new AssemblyReader();
            Assembly ass;
            int retCode;

            try
            {
                ass = reader.LoadFrom(asspath);
                Type t = ass.GetType(type);

                if (t == null)
                {
                    Console.WriteLine("Error: Type {0} not found in assembly {1}", type, asspath);
                    return 1;
                }

                if (html) await SyntaxWriter.DisassembleTypeAsHtmlAsync(t, new DisassemblerParams(), target);
                else await SyntaxWriter.DisassembleTypeAsync(t, new DisassemblerParams(), target);

                retCode = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(ex.ToString());
                retCode = 1;
            }
            finally
            {
                reader.Dispose();
            }

            return retCode;
        }

        public override int Execute(string[] args)
        {
            string asspath = string.Empty;
            string type = string.Empty;
            string method = string.Empty;
            string outpath = null;
            bool html = false;
            string targExt = ".il";

            if (args.Length < 2)
            {
                Console.WriteLine("Error: not enough arguments for 'disasm' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            // Parse command line arguments
            NamedArgumentDefinition[] defs = new NamedArgumentDefinition[]
            {
                new NamedArgumentDefinition("--output", true, "Output file path"),
                new NamedArgumentDefinition("--html", false, "Output format is HTML"),
            };

            CommandLineArgs cla = new CommandLineArgs(args, defs);
            
            if (cla.HasNamedArgument("--output"))
            {
                outpath = cla.GetNamedArgument("--output");
            }

            if (cla.HasNamedArgument("--html"))
            {
                html = true;
                targExt = ".html";
            }

            if (cla.PositionalArgumentsCount > 1)
            {
                asspath = cla.GetPositionalArgument(1);
            }

            if (string.IsNullOrEmpty(asspath))
            {
                Console.WriteLine("Error: Assembly path is not provided for the 'disasm' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            if (string.IsNullOrEmpty(outpath))
            {
                outpath = Path.GetFileNameWithoutExtension(asspath) + targExt;
            }

            if (cla.PositionalArgumentsCount > 2)
            {
                type = cla.GetPositionalArgument(2);
            }

            if (cla.PositionalArgumentsCount > 3)
            {
                method = cla.GetPositionalArgument(3);
            }

            Console.WriteLine("Input file: " + asspath);
            StreamWriter wr;

            try
            {
                wr = new StreamWriter(outpath, append: false, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: Cannot open output path " + outpath);
                Console.WriteLine(ex.ToString());
                return 1;
            }

            // Disassemble
            using (wr)
            {
                int res;
                Console.WriteLine("Disassembling CIL...");

                if (string.IsNullOrEmpty(type))
                {
                    // disassemble assembly
                    Task<int> task = DisassembleAssembly(asspath, html, wr);
                    task.Wait(); //OK to block in console app

                    if (task.IsCompletedSuccessfully) res = task.Result;
                    else res = 1;

                    if (res == 0) Console.WriteLine("Output successfully written to " + outpath);
                    else Console.WriteLine("Failed to disassemble");

                    return res;
                }
                
                if (string.IsNullOrEmpty(method))
                {
                    //disassemble type
                    Task<int> task = DisassembleType(asspath, type, html, wr);
                    task.Wait(); //OK to block in console app

                    if (task.IsCompletedSuccessfully) res = task.Result;
                    else res = 1;
                }
                else
                {
                    //disassemble method
                    res = Visualizer.DisassembleMethod(asspath, type, method, html, wr);
                }

                if (res == 0) Console.WriteLine("Output successfully written to " + outpath);
                else Console.WriteLine("Failed to disassemble");

                return res;
            }
        }
    }
}
