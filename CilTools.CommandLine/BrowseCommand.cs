/* CIL Tools 
 * Copyright (c) 2024,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using CilView.Core.Documentation;
using System;
using System.Collections.Generic;
using System.Text;
using CilView.UI;

namespace CilTools.CommandLine
{
    class BrowseCommand : Command
    {
        public override string Name => "browse";

        public override string Description => "";

        public override IEnumerable<TextParagraph> UsageDocumentation => new TextParagraph[0];

        public override bool IsHidden => true;

        public override int Execute(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Error: not enough arguments for 'browse' command.");
                Console.WriteLine(CLI.GetErrorInfo());
                return 1;
            }

            string assemblyPath = args[1];
            AppBuilder.BuildApplication(assemblyPath).RunInBackground();
            Console.ReadKey();

            return 0;
        }
    }
}
