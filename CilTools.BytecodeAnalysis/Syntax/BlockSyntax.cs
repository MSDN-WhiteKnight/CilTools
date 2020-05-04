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
    public class BlockSyntax:SyntaxElement
    {
        string _header;
        List<SyntaxElement> _children;
        
        internal List<SyntaxElement> ChildArray { get { return this._children; } set { this._children = value; } }

        internal BlockSyntax(string header, SyntaxElement[] children)
        {
            if (header == null) header = "";

            this._header = header;
            this._children = new List<SyntaxElement>(children);
        }

        public string Header { get { return this._header; } }

        public IEnumerable<SyntaxElement> Children
        {
            get 
            {
                for(int i=0;i<_children.Count;i++)
                {
                    yield return _children[i];
                }
            }
        }

        public override void ToText(TextWriter target)
        {
            target.Write(this._header);
            target.Write(' ');
            target.Write('{');

            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].ToText(target);
                target.WriteLine();
            }

            target.Write('}');
            target.Flush();
        }
    }
}
