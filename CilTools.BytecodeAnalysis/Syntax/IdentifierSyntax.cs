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
    /// Represents identifier in CIL assembler. Identifier is a name of the member or variable.
    /// </summary>
    public class IdentifierSyntax : SyntaxNode
    {
        string _content;
        bool _ismember;

        /// <summary>
        /// Gets the content of this identifier as string
        /// </summary>
        public string Content { get { return this._content; } }

        /// <summary>
        /// Gets the value indicating whether this identifier represents assembly member name
        /// </summary>
        public bool IsMemberName { get { return this._ismember; } }

        internal IdentifierSyntax(string lead, string content, string trail, bool ismember)
        {
            if (lead == null) lead = "";
            if (trail == null) trail = "";
            this._lead = lead;
            this._content = content;
            this._trail = trail;
            this._ismember = ismember;
        }

        /// <inheritdoc/>
        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            target.Write(this._lead);
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
