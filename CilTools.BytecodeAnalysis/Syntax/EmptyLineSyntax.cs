/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using CilTools.BytecodeAnalysis;

namespace CilTools.Syntax
{
    public class EmptyLineSyntax:SyntaxElement
    {
        internal EmptyLineSyntax()
        {
            //do nothing
        }

        public override void ToText(TextWriter target)
        {
            //do nothing
        }
    }
}
