/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CilTools.Syntax;
using CilView.Common;

namespace CilView.Core.Syntax
{
    /// <summary>
    /// Represent a syntax tree parsed from a source document (either whole document or some subtree)
    /// </summary>
    public class DocumentSyntax : SyntaxNode
    {
        List<SyntaxNode> _children;
        string _name;

        public DocumentSyntax(IEnumerable<SyntaxNode> children, string name)
        {
            this._children = new List<SyntaxNode>(children);
            this._name = name;
        }

        public DocumentSyntax(IEnumerable<SyntaxNode> children)
        {
            this._children = new List<SyntaxNode>(children);
            this._name = string.Empty;
        }

        public DocumentSyntax(string name)
        {
            this._children = new List<SyntaxNode>(100);
            this._name = name;
        }

        public void Add(SyntaxNode node)
        {
            this._children.Add(node);
        }

        public string Name
        {
            get { return this._name; }
        }

        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            return this._children.ToArray();
        }

        public override void ToText(TextWriter target)
        {
            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].ToText(target);
            }

            target.Flush();
        }
    }
}
