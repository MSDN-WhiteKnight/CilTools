/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.CommandLine
{
    class HelpCommand : Command
    {
        public override string Name
        {
            get { return "help"; }
        }

        public override string Description
        {
            get { return "Print available commands"; }
        }

        public override string UsageDocumentation
        {
            get { return string.Empty; }
        }

        public override int Execute(string[] args)
        {
            Console.WriteLine(" Commands:");
            Console.WriteLine();

            foreach (string key in Command.All.Keys)
            {
                Command cmd = Command.All[key];

                if (cmd.IsHidden) continue;

                Console.Write(key);
                Console.Write(" - ");
                Console.WriteLine(cmd.Description);
                Console.WriteLine();

                if (cmd.UsageDocumentation.Length > 0)
                {
                    Console.WriteLine(" Usage");
                    Console.WriteLine(cmd.UsageDocumentation);
                }
            }
            
            return 0;
        }
    }
}
