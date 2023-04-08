/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.Syntax;

namespace CilTools.Tests.Common
{
    public class DisassemblerSampleType
    {
        public static int x;

        public static void Test()
        {
            Console.WriteLine("Hello, World");
        }
    }

    public class DerivedSampleType : DisassemblerSampleType { }

    public class PropertySampleType : SyntaxFactory
    {
        public int X { get { return 0; } }

        public override SyntaxNode CreateNode(string content, string leadingWhitespace, string trailingWhitespace)
        {
            throw new NotImplementedException();
        }
    }
}
