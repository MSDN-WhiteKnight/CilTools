/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CilTools.Metadata;
using CilTools.Syntax;
using CilView.Common;
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
                    " disasm [--output <output path>] <assembly path> [<type full name>] [<method name>]");
                yield return TextParagraph.Text("[--output <output path>] - Output file path");
            }
        }

        static async Task<int> DisassembleAssembly(string asspath, TextWriter target)
        {
            AssemblyReader reader = new AssemblyReader();
            Assembly ass;
            int retCode;

            try
            {
                ass = reader.LoadFrom(asspath);
                await SyntaxWriter.DisassembleAsync(ass, new DisassemblerParams(), target);
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

        static async Task<int> DisassembleType(string asspath, string type, TextWriter target)
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

                await SyntaxWriter.DisassembleTypeAsync(t, new DisassemblerParams(), target);
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

        static int DisassembleMethod(string asspath, string type, string method, TextWriter target)
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

                MemberInfo[] methods = Utils.GetAllMembers(t);
                Func<MethodBase, bool> predicate = (x) => Utils.StringEquals(x.Name, method);
                MethodBase[] selectedMethods = methods.OfType<MethodBase>().Where(predicate).ToArray();

                if (selectedMethods.Length == 0)
                {
                    Console.WriteLine("Error: Type {0} does not declare methods with the specified name", type);
                    return 1;
                }

                SyntaxWriter.WriteHeader(target);

                for (int i = 0; i < selectedMethods.Length; i++)
                {
                    SyntaxWriter.DisassembleMethod(selectedMethods[i], new DisassemblerParams(), target);
                    target.WriteLine();
                }

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
            string asspath;
            string type;
            string method;
            string outpath = null;

            if (args.Length < 2)
            {
                Console.WriteLine("Error: not enough arguments for 'disasm' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            int pos = 1;

            if (CLI.TryReadExpectedParameter(args, pos, "--output"))
            {
                pos++;
                outpath = CLI.ReadCommandParameter(args, pos);
                pos++;
            }

            asspath = CLI.ReadCommandParameter(args, pos);
            pos++;

            if (string.IsNullOrEmpty(asspath))
            {
                Console.WriteLine("Error: Assembly path is not provided for the 'disasm' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            if (string.IsNullOrEmpty(outpath))
            {
                outpath = Path.GetFileNameWithoutExtension(asspath) + ".il";
            }

            type = CLI.ReadCommandParameter(args, pos);
            pos++;
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

            using (wr)
            {
                int res;
                Console.WriteLine("Disassembling CIL...");

                if (string.IsNullOrEmpty(type))
                {
                    // disassemble assembly
                    Task<int> task = DisassembleAssembly(asspath, wr);
                    task.Wait(); //OK to block in console app

                    if (task.IsCompletedSuccessfully) res = task.Result;
                    else res = 1;

                    if (res == 0) Console.WriteLine("Output successfully written to " + outpath);
                    else Console.WriteLine("Failed to disassemble");

                    return res;
                }

                method = CLI.ReadCommandParameter(args, pos);

                if (string.IsNullOrEmpty(method))
                {
                    //disassemble type
                    Task<int> task = DisassembleType(asspath, type, wr);
                    task.Wait(); //OK to block in console app

                    if (task.IsCompletedSuccessfully) res = task.Result;
                    else res = 1;
                }
                else
                {
                    //disassemble method
                    res = DisassembleMethod(asspath, type, method, wr);
                }

                if (res == 0) Console.WriteLine("Output successfully written to " + outpath);
                else Console.WriteLine("Failed to disassemble");

                return res;
            }
        }
    }
}
