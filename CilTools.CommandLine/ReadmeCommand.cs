/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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

        public override IEnumerable<TextParagraph> UsageDocumentation
        {
            get { return new TextParagraph[0]; }
        }

        public override bool IsHidden => true;

        public override int Execute(string[] args)
        {
            FileStream fs = null;
            StreamWriter wr = null;

            try
            {
                fs = new FileStream("readme.md", FileMode.Create, FileAccess.Write, FileShare.Read);
                wr = new StreamWriter(fs, Encoding.UTF8);
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
                    TextParagraph[] usage = cmd.UsageDocumentation.ToArray();

                    if (usage.Length > 0)
                    {
                        wr.WriteLine("*Usage*");
                        wr.WriteLine();

                        for (int i = 0; i < usage.Length; i++)
                        {
                            string md = usage[i].GetMarkdown();
                            wr.WriteLine(md);
                            if (md.Length > 0) wr.WriteLine();
                        }
                    }
                }

                wr.WriteLine("---");
                wr.WriteLine();
                wr.WriteLine(CLI.Copyright);
                Console.WriteLine("Generated readme.md");

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }
            finally
            {
                if (wr != null) wr.Dispose();
                if (fs != null) fs.Dispose();
            }
        }
    }
}
