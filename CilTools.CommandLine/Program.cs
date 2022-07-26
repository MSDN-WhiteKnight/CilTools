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
        static Dictionary<string,Command> commands;
        
        static void PrintHelp()
        {
            Console.WriteLine(" Commands:");
            Console.WriteLine();
            
            foreach(string key in commands.Keys)
            {
                Console.Write(key);
                Console.Write(" - ");
                Console.WriteLine(commands[key].Description);
                Console.WriteLine();
                Console.WriteLine(" Usage");
                Console.WriteLine(commands[key].UsageDocumentation);
            }
            
            Console.WriteLine("help - Print available commands");
            Console.WriteLine();
        }
                
        static int Main(string[] args)
        {
            Console.WriteLine("*** CIL Tools command line ***");
            Console.WriteLine("Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight/CilTools)");
            Console.WriteLine();
            
            //commands list
            commands = new Dictionary<string,Command>(2);
            commands["view"] = new ViewCommand();
            commands["disasm"] = new DisasmCommand();

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
            else if (commands.ContainsKey(args[0]))
            {
                return commands[args[0]].Execute(args);
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
