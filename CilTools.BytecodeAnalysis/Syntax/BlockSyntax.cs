/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Syntax
{
    /// <summary>
    /// Represents the block syntax in the CIL assembler. The block contains child directive and instruction nodes.
    /// </summary>
    /// <remarks>
    /// The examples of block syntax are method body or exception handler blocks. The block consists of the optional header, 
    /// opening curly brace, content nodes (directives and statements) and closing curly brace.
    /// </remarks>
    public class BlockSyntax : SyntaxNode
    {
        string _indent;
        SyntaxNode[] _header;
        List<SyntaxNode> _children;

        internal BlockSyntax(string indent, SyntaxNode[] header, SyntaxNode[] children)
        {
            if (indent == null) indent = string.Empty;
            if (header == null) header = SyntaxNode.EmptySyntax;

            this._indent = indent;
            this._header = header;
            this._children = new List<SyntaxNode>(children);

            for (int i = 0; i < this._header.Length; i++) this._header[i]._parent = this;

            for (int i = 0; i < this._children.Count; i++) this._children[i]._parent = this;
        }

        /// <summary>
        /// Gets the header syntax of this block, or an empty collection if this block has no header
        /// </summary>
        /// <remarks>
        /// For the exception handler blocks, the header represents the exception clause. Other types of blocks does not have headers.
        /// </remarks>
        public IEnumerable<SyntaxNode> HeaderSyntax
        {
            get
            {
                for (int i = 0; i < this._header.Length; i++) yield return this._header[i];
            }
        }

        /// <summary>
        /// Returns the collection of the syntax nodes contained within this block (content nodes)
        /// </summary>
        /// <remarks>
        /// The content nodes are located between opening and closing curly braces. They include instructions and directives.
        /// </remarks>
        public IEnumerable<SyntaxNode> Content
        {
            get
            {
                for (int i = 0; i < _children.Count; i++)
                {
                    yield return _children[i];
                }
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            if (this._header.Length > 0)
            {
                yield return new GenericSyntax(this._indent) { _parent = this };

                for (int i = 0; i < _header.Length; i++)
                {
                    yield return _header[i];
                }

                yield return new PunctuationSyntax(Environment.NewLine + this._indent, "{", Environment.NewLine)
                {
                    _parent = this
                };
            }
            else
            {
                yield return new PunctuationSyntax(this._indent, "{", Environment.NewLine)
                {
                    _parent = this
                };
            }

            for (int i = 0; i < _children.Count; i++)
            {
                yield return _children[i];
            }

            yield return new PunctuationSyntax(this._indent, "}", Environment.NewLine)
            {
                _parent = this
            };
        }

        internal void AddChildNode(SyntaxNode node)
        {
            this._children.Add(node);
            node._parent = this;
        }
    }
}
