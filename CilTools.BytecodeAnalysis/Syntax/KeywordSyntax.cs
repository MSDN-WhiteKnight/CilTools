/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CilTools.BytecodeAnalysis;

namespace CilTools.Syntax
{
    /// <summary>
    /// Represents the keyword token in CIL assembler. 
    /// </summary>
    /// <remarks> 
    /// The keyword is a special sequence of characters that can't be used as identifier. 
    /// The list of keywords is predefined by specification. 
    /// The keyword could represent the directive name, access modifier, instruction name 
    /// or other special syntactic element.
    /// </remarks>
    public class KeywordSyntax:SyntaxNode
    {
        string _content;
        KeywordKind _kind;

        /// <summary>
        /// Gets the value of this keyword as string
        /// </summary>
        public string Content { get { return this._content; } }

        /// <summary>
        /// Gets the keyword kind
        /// </summary>
        public KeywordKind Kind { get { return this._kind; } }

        internal KeywordSyntax(string lead, string content, string trail, KeywordKind kind)
        {
            if (lead == null) lead = "";
            if (trail == null) trail = "";

            this._lead = lead;
            this._content = content;
            this._trail = trail;
            this._kind = kind;
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
