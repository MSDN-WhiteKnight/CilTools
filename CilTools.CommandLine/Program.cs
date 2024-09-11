/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.CommandLine
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("*** CIL Tools command line ***");
            Console.WriteLine(CLI.Copyright);
            Console.WriteLine();

            //register commands
            Command.All["view"] = new ViewCommand();
            Command.All["disasm"] = new DisasmCommand();
            Command.All["view-source"] = new ViewSourceCommand();
            Command.All["fileinfo"] = new FileInfoCommand();
            Command.All["browse"] = new BrowseCommand();
            Command.All["help"] = new HelpCommand();
            Command.All["readme"] = new ReadmeCommand();

            //read and process command line arguments
            if (args.Length == 0)
            {
                Command.All["help"].Execute(args);
                return 1;
            }
            else if (args[0] == "/?" || args[0] == "-?")
            {
                return Command.All["help"].Execute(args);
            }
            else if (Command.All.ContainsKey(args[0]))
            {
                return Command.All[args[0]].Execute(args);
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
