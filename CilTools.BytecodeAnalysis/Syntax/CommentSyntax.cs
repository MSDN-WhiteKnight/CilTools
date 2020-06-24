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
    /// <summary>
    /// Represents the comment in the CIL assembler code. The comment does not impact code semantics in any way, but may provide extra information.
    /// </summary>
    public class CommentSyntax:SyntaxNode
    {
        string _content;

        internal CommentSyntax(string lead,string content)
        {
            if (lead == null) lead = "";
            this._lead = lead;
            this._content = content;
            this._trail = Environment.NewLine;
        }

        /// <inheritdoc/>
        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            target.Write(this._lead);
            target.Write('/');
            target.Write('/');
            target.Write(this._content);
            target.Write(this._trail);
            target.Flush();
        }

        /// <inheritdoc/>
        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            return SyntaxNode.EmptyArray;
        }
    }
}
