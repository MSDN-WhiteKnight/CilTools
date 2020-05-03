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
    public class CommentNode:SyntaxTreeNode
    {
        string _content;

        internal CommentNode(string content)
        {
            this._content = content;
        }

        public override IEnumerable<SyntaxTreeNode> Children
        {
            get { return new SyntaxTreeNode[] { }; }
        }

        public override void ToText(TextWriter target)
        {
            target.Write(this._content);
            target.Flush();
        }
    }
}
