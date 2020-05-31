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
        string _header;
        List<SyntaxNode> _children;
        
        internal List<SyntaxNode> ContentArray { get { return this._children; } set { this._children = value; } }

        internal BlockSyntax(string lead, string header, SyntaxNode[] children)
        {
            if (lead == null) lead = "";
            if (header == null) header = "";

            this._lead = lead;
            this._header = header;
            this._children = new List<SyntaxNode>(children);
        }

        public string Header { get { return this._header; } }

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
                target.Write(this._lead);
                target.WriteLine(this._header);
                target.Write(this._lead);
                target.WriteLine('{');
            }
            else
            {
                target.Write(this._lead);
                target.WriteLine('{');
            }

            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].ToText(target);
            }

            target.Write(this._lead);
            target.WriteLine('}');
            target.Flush();
        }

        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            if (this._header.Length > 0)
            {
                yield return new GenericSyntax(this._lead + this._header);
                yield return new PunctuationSyntax(Environment.NewLine + this._lead, "{", Environment.NewLine);
            }
            else
            {
                yield return new PunctuationSyntax(this._lead, "{", Environment.NewLine);
            }

            for (int i = 0; i < _children.Count; i++)
            {
                yield return _children[i];
            }

            yield return new PunctuationSyntax(this._lead, "}", Environment.NewLine);
        }
    }
}
