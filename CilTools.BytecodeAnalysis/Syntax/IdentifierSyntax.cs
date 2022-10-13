﻿/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Syntax
{
    /// <summary>
    /// Represents identifier in CIL assembler. Identifier is a name of the member or variable.
    /// </summary>
    /// <remarks>
    /// Starting from the version 2.3, identifiers that overlap with CIL assembler keywords are 
    /// escaped when converting to text. For example, if the method is called <c>method</c>, 
    /// its name will be represented like <c>'method'</c> in text output.
    /// </remarks>
    public class IdentifierSyntax : SyntaxNode
    {
        string _content;
        bool _ismember;
        object _target = null;
        string _contentEscaped;

        /// <summary>
        /// Gets the content of this identifier as string
        /// </summary>
        public string Content { get { return this._content; } }

        /// <summary>
        /// Gets the value indicating whether this identifier represents assembly member name
        /// </summary>
        public bool IsMemberName { get { return this._ismember; } }

        /// <summary>
        /// Gets the item (such as assembly, member or variable) that this identifier represents
        /// </summary>
        /// <value>
        /// The reference to the target item, or <c>null</c> if the target item is unknown
        /// </value>
        public object TargetItem { get { return this._target; } }

        /// <summary>
        /// Gets the assembly member represented by this identifier
        /// </summary>
        /// /// <value>
        /// The reference to the target member, or <c>null</c> if the target item is unknown or not a member
        /// </value>
        public MemberInfo TargetMember { get { return this._target as MemberInfo; } }

        internal IdentifierSyntax(string lead, string content, string trail, bool ismember,object target)
        {
            if (lead == null) lead = "";
            if (trail == null) trail = "";
            this._lead = lead;
            this._content = content;
            this._trail = trail;
            this._ismember = ismember;
            this._target = target;
            this._contentEscaped = SyntaxUtils.EscapeIdentifier(content);
        }

        /// <summary>
        /// Writes text representation of this node into the specified TextWriter
        /// </summary>
        /// <remarks>
        /// Starting from the version 2.3, identifiers that overlap with CIL assembler keywords are 
        /// escaped when converting to text. For example, if the method is called <c>method</c>, 
        /// its name will be represented like <c>'method'</c> in text output.
        /// </remarks>
        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            target.Write(this._lead);
            target.Write(this._contentEscaped);
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
