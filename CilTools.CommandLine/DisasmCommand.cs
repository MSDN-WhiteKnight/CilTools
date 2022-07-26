/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CilView.Core;
using CilView.Core.Syntax;

namespace CilTools.CommandLine
{
    class DisasmCommand : Command
    {
        public override string Name {get{return "disasm";}}
        
        public override string Description {get{return "Write disassembled CIL code of the specified type or method into the file";}}
        
        public override string UsageDocumentation {get{
            string exeName = typeof(Program).Assembly.GetName().Name;
            StringBuilder sb=new StringBuilder(1000);
            sb.AppendLine(exeName + " disasm [--output <output path>] <assembly path> <type full name> [<method name>]");
            sb.AppendLine("[--output <output path>] - Output file path");
            return sb.ToString();
        }}
        
        public override int Execute(string[] args)
        {
            string asspath;
            string type;
            string method;
            string outpath=null;

            if (args.Length < 3)
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

            if (type == null)
            {
                Console.WriteLine("Error: Type name is not provided for the 'disasm' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
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

            using (wr)
            {
                Console.WriteLine("Disassembling CIL...");
                method = CLI.ReadCommandParameter(args, pos);
                int res;

                if (string.IsNullOrEmpty(method))
                {
                    //disassemble type
                    res = Disassembler.DisassembleType(asspath, type, full: true, noColor: true, wr);
                }
                else
                {
                    //disassemble method
                    res = Disassembler.DisassembleMethod(asspath, type, method, noColor: true, wr);
                }

                if (res == 0) Console.WriteLine("Output successfully written to " + outpath);
                else Console.WriteLine("Failed to disassemble");

                return res;
            }
        }
    }
}
