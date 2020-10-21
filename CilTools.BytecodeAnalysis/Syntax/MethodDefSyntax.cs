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
    /// Represents the syntax of the method definition in CIL assembler
    /// </summary>
    /// <remarks>
    /// Method definition consists of the signature ("header") and the body. The body can contain directives 
    /// (such as custom attributes or local variable declarations) and instructions.
    /// </remarks>
    public class MethodDefSyntax : SyntaxNode
    {
        DirectiveSyntax _sig;
        BlockSyntax _body;

        /// <summary>
        /// Gets the directive representing the signature of the defined method
        /// </summary>
        public DirectiveSyntax Signature
        {
            get { return this._sig; }
        }

        /// <summary>
        /// Gets the block that forms the body of the defined method
        /// </summary>
        public BlockSyntax Body
        {
            get { return this._body; }
        }

        internal MethodDefSyntax(DirectiveSyntax sig, BlockSyntax body)
        {
            this._sig = sig;
            this._body = body;
            this._sig._parent = this;
            this._body._parent = this;
        }

        /// <inheritdoc/>
        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");
            
            this._sig.ToText(target);
            this._body.ToText(target);
        }

        /// <inheritdoc/>
        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            yield return this._sig;
            yield return this._body;
        }
    }
}
