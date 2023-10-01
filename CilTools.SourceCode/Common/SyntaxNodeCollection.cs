/* CIL Tools 
 * Copyright (c) 2023, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CilTools.Syntax;

namespace CilTools.SourceCode.Common
{
    /// <summary>
    /// Represents an ordered collection of syntax nodes
    /// </summary>
    public class SyntaxNodeCollection : SyntaxNode
    {
        // When used with SourceToken instances, tracks element ordinal numbers to enable getting previous/next node

        SyntaxNode[] _nodes;

        private SyntaxNodeCollection() { }

        /// <summary>
        /// Creates a new <c>SyntaxNodeCollection</c> using contents of the specified array
        /// </summary>
        public static SyntaxNodeCollection FromArray(SyntaxNode[] nodes)
        {
            SyntaxNodeCollection ret = new SyntaxNodeCollection();
            ret._nodes = nodes;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] is SourceToken)
                {
                    SourceToken st = (SourceToken)nodes[i];
                    st.SetParent(ret);
                    st.OrdinalNumber = i;
                }
            }

            return ret;
        }

        /// <summary>
        /// Gets the number of nodes in this collection
        /// </summary>
        public int Count
        {
            get { return _nodes.Length; }
        }

        /// <inheritdoc/>
        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            for (int i = 0; i < _nodes.Length; i++) yield return _nodes[i];
        }

        /// <inheritdoc/>
        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            for (int i = 0; i < _nodes.Length; i++) _nodes[i].ToText(target);
        }

        /// <summary>
        /// Gets the syntax node at the specified index in this collection
        /// </summary>
        public SyntaxNode GetNode(int index)
        {
            return this._nodes[index];
        }
    }
}
