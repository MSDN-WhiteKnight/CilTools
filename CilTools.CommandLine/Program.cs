/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CilTools.CommandLine
{
    class Program
    {
        static void PrintHelp()
        {
            string exeName = typeof(Program).Assembly.GetName().Name;
            Console.WriteLine(" Commands:");
            Console.WriteLine();

            Console.WriteLine("view - Print CIL code of types or methods or the content of CIL source files");
            Console.WriteLine();
            Console.WriteLine(" Usage");
            Console.WriteLine("Print disassembled CIL code of the specified type or method:");
            Console.WriteLine("   " + exeName + " view [--nocolor] <assembly path> <type full name> [<method name>]");
            Console.WriteLine("Print contents of the specified CIL source file (*.il):");
            Console.WriteLine("   " + exeName + " view [--nocolor] <source file path>");
            Console.WriteLine();
            Console.WriteLine("[--nocolor] - disable syntax highlighting");
            Console.WriteLine();

            Console.WriteLine("disasm - Write disassembled CIL code of the specified type or method into the file");
            Console.WriteLine("Usage: " + exeName + 
                " disasm [--output <output path>] <assembly path> <type full name> [<method name>]");
            Console.WriteLine("[--output <output path>] - Output file path");
            Console.WriteLine();

            Console.WriteLine("help - Print available commands");
            Console.WriteLine();
        }
                
        static int Main(string[] args)
        {
            Console.WriteLine("*** CIL Tools command line ***");
            Console.WriteLine("Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight/CilTools)");
            Console.WriteLine();

            //read and process command line arguments

            if (args.Length == 0)
            {
                PrintHelp();
                return 1;
            }
            else if (args[0] == "help" || args[0] == "/?" || args[0] == "-?")
            {
                PrintHelp();
                return 0;
            }
            else if (args[0] == "view")
            {
                return new ViewCommand().Execute(args);
            }
            else if (args[0] == "disasm") 
            {
                return new DisasmCommand().Execute(args);
            }
            else
            {
                Console.WriteLine("Error: unknown command " + args[0]);
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }
        }
    }
}
