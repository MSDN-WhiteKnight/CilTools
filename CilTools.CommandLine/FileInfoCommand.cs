/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Metadata;
using CilView.Core.Documentation;
using CilView.Core.Reflection;

namespace CilTools.CommandLine
{
    class FileInfoCommand : Command
    {
        public override string Name => "fileinfo";

        public override string Description => "Prints information about assembly file";

        public override IEnumerable<TextParagraph> UsageDocumentation
        {
            get
            {
                string exeName = typeof(Program).Assembly.GetName().Name;

                yield return TextParagraph.Code("    " + exeName +
                    " fileinfo <assembly path>");
            }
        }

        public override int Execute(string[] args)
        {
            string filepath;

            if (args.Length < 2)
            {
                Console.WriteLine("Error: not enough arguments for 'fileinfo' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            filepath = CLI.ReadCommandParameter(args, 1);

            if (string.IsNullOrEmpty(filepath))
            {
                Console.WriteLine("Error: File path is not provided for the 'fileinfo' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }
            
            AssemblyReader reader = new AssemblyReader();

            try
            {
                Assembly ass = reader.LoadFrom(filepath);
                string s = AssemblyInfoProvider.GetAssemblyInfo(ass);
                Console.WriteLine(s);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }
            finally { reader.Dispose(); }

            return 0;
        }
    }
}
