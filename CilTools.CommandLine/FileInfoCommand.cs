/* CIL Tools
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using CilTools.Metadata;
using CilView.Core;
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
            string filepath = string.Empty;

            if (args.Length < 2)
            {
                Console.WriteLine("Error: not enough arguments for 'fileinfo' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            // Parse command line arguments
            CommandLineArgs cla = new CommandLineArgs(args, new NamedArgumentDefinition[0]);

            if (cla.PositionalArgumentsCount > 1)
            {
                //read path for assembly
                filepath = cla.GetPositionalArgument(1);
            }
            
            if (string.IsNullOrEmpty(filepath))
            {
                Console.WriteLine("Error: File path is not provided for the 'fileinfo' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            if (!File.Exists(filepath) && FileUtils.IsFileNameWithoutDirectory(filepath) &&
                filepath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                // If full path is not specified, try to find path for BCL assembly of the current runtime
                string bclPath = FileUtils.GetBclAssemblyPath(filepath);

                if (File.Exists(bclPath)) filepath = bclPath;
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
