/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace CilTools.CommandLine
{
    class ReadmeCommand : Command
    {
        public override string Name
        {
            get { return "readme"; }
        }

        public override string Description
        {
            get { return "Generate readme"; }
        }

        public override string UsageDocumentation
        {
            get { return string.Empty; }
        }

        public override bool IsHidden => true;

        static string EscapeForMarkdown(string str)
        {
            string ret = str.Replace("\r\n", "\r\n\r\n");
            ret = ret.Replace("<", "\\<");
            ret = ret.Replace(">", "\\>");
            return ret;
        }

        public override int Execute(string[] args)
        {
            FileStream fs = new FileStream("readme.md", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            StreamWriter wr = new StreamWriter(fs, Encoding.UTF8);

            using (wr)
            {
                wr.WriteLine("# CIL Tools command line");
                wr.WriteLine();
                wr.WriteLine("Command line tool to view disassembled CIL code of methods in .NET assemblies.");
                wr.WriteLine();
                wr.WriteLine("Commands:");
                wr.WriteLine();

                foreach (string key in Command.All.Keys)
                {
                    Command cmd = Command.All[key];

                    if (cmd.IsHidden) continue;

                    wr.Write('*');
                    wr.Write('*');
                    wr.Write(key);
                    wr.Write("** - ");
                    wr.WriteLine(cmd.Description);
                    wr.WriteLine();

                    if (cmd.UsageDocumentation.Length > 0)
                    {
                        wr.WriteLine("*Usage*");
                        wr.WriteLine();
                        wr.WriteLine(EscapeForMarkdown(cmd.UsageDocumentation));
                    }
                }

                wr.WriteLine("---");
                wr.WriteLine();
                wr.WriteLine(CLI.Copyright);
                wr.WriteLine();

                return 0;
            }
        }
    }
}
