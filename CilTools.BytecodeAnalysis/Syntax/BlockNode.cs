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
    public class BlockNode:SyntaxTreeNode
    {
        SyntaxTreeNode[] _children;

        internal BlockNode(SyntaxTreeNode[] children)
        {
            this._children = children;
        }

        public override IEnumerable<SyntaxTreeNode> Children
        {
            get 
            {
                for(int i=0;i<_children.Length;i++)
                {
                    yield return _children[i];
                }
            }
        }

        public override void ToText(TextWriter target)
        {
            target.Write('{');

            for (int i = 0; i < _children.Length; i++)
            {
                _children[i].ToText(target);
                target.WriteLine();
            }

            target.Write('}');
            target.Flush();
        }
    }
}
