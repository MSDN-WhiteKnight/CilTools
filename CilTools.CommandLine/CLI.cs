/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CilTools.CommandLine
{
    static class CLI
    {
        public static string GetErrorInfo()
        {
            string exeName = typeof(Program).Assembly.GetName().Name;
            return "Use \"" + exeName + " help\" to print the list of available commands and their arguments.";
        }
        
        public static bool TryReadExpectedParameter(string[] args, int pos, string expected)
        {
            if (pos >= args.Length) return false;

            if (args[pos] == expected) return true;
            else return false;
        }

        public static string ReadCommandParameter(string[] args, int pos)
        {
            if (pos >= args.Length) return null;

            return args[pos];
        }
    }
}
