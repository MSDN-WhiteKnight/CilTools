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
    public class BlockEndSyntax : SyntaxElement
    {
        internal BlockEndSyntax(string lead)
        {
            if (lead == null) lead = "";

            this._lead = lead;
        }

        public override void ToText(TextWriter target)
        {
            target.Write(this._lead);
            target.Write('}');
        }
    }
}
