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
    public class CommentSyntax:SyntaxNode
    {
        string _content;

        internal CommentSyntax(string lead,string content)
        {
            if (lead == null) lead = "";
            this._lead = lead;
            this._content = content;
        }

        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            target.Write(this._lead);
            target.Write('/');
            target.Write('/');
            target.Write(this._content);
            target.Flush();
        }
    }
}
