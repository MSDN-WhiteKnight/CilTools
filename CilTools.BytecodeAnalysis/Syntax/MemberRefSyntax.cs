/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CilTools.BytecodeAnalysis;

namespace CilTools.Syntax
{
    /// <summary>
    /// Represents the reference to the assembly member (such as type, method or field) in CIL assembler
    /// </summary>
    /// <remarks>
    /// The member reference syntax is used as instruction operand or in variable declarations. 
    /// </remarks>
    public class MemberRefSyntax:SyntaxNode
    {
        MemberInfo _member;
        SyntaxNode[] _content;

        /// <summary>
        /// Gets the reflection construct representing the referenced member
        /// </summary>
        public MemberInfo Member { get { return this._member; } }

        internal MemberRefSyntax(SyntaxNode[] content, MemberInfo m)
        {
            this._content = content;
            this._member = m;

            for (int i = 0; i < this._content.Length; i++) this._content[i].SetParent(this);
        }

        /// <inheritdoc/>
        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");
            
            for (int i = 0; i < this._content.Length; i++) this._content[i].ToText(target);
            
            target.Flush();
        }

        /// <inheritdoc/>
        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            for (int i = 0; i < this._content.Length; i++) yield return this._content[i]; 
        }
    }
}
