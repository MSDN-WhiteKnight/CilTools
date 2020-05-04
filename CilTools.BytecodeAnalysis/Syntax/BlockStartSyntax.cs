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
    public class BlockStartSyntax:SyntaxElement
    {
        string _header;

        internal BlockStartSyntax(string lead, string header)
        {
            if (lead == null) lead = "";
            if (header == null) header = "";

            this._lead = lead;
            this._header = header;
        }

        public override void ToText(TextWriter target)
        {
            target.Write(this._lead);
            target.Write(this._header);
            target.Write(' ');
            target.Write('{');
        }
    }
}
