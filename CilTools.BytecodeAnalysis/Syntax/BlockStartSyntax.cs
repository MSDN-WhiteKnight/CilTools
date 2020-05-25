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
    public class BlockStartSyntax:SyntaxNode
    {
        string _header;

        internal BlockStartSyntax(string lead, string header)
        {
            if (lead == null) lead = "";
            if (header == null) header = "";

            this._lead = lead;
            this._header = header;
        }

        public void WriteHeader(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");
            
            target.Write(this._lead);

            if (this._header.Length > 0)
            {
                target.Write(this._header);
                target.Write(' ');
            }
        }

        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            this.WriteHeader(target);
            target.Write('{');
        }
    }
}
