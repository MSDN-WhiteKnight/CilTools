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
    public class BlockSyntax:SyntaxNode
    {
        string _indent;
        SyntaxNode[] _header;
        internal List<SyntaxNode> _children;
        
        internal BlockSyntax(string indent, SyntaxNode[] header, SyntaxNode[] children)
        {
            if (indent == null) indent = "";
            if (header == null) header = SyntaxNode.EmptySyntax;

            this._indent = indent;
            this._header = header;
            this._children = new List<SyntaxNode>(children);
        }

        public SyntaxNode[] HeaderSyntax { get { return this._header; } }

        public IEnumerable<SyntaxNode> Content
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
            if (this._header.Length > 0)
            {
                target.Write(this._indent);

                for (int i = 0; i < this._header.Length; i++) _header[i].ToText(target);

                target.WriteLine();
                target.Write(this._indent);
                target.WriteLine('{');
            }
            else
            {
                target.Write(this._indent);
                target.WriteLine('{');
            }

            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].ToText(target);
            }

            target.Write(this._indent);
            target.WriteLine('}');
            target.Flush();
        }

        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            if (this._header.Length > 0)
            {
                yield return new GenericSyntax(this._indent);

                for (int i = 0; i < _header.Length; i++)
                {
                    yield return _header[i];
                }

                yield return new PunctuationSyntax(Environment.NewLine + this._indent, "{", Environment.NewLine);
            }
            else
            {
                yield return new PunctuationSyntax(this._indent, "{", Environment.NewLine);
            }

            for (int i = 0; i < _children.Count; i++)
            {
                yield return _children[i];
            }

            yield return new PunctuationSyntax(this._indent, "}", Environment.NewLine);
        }
    }
}
