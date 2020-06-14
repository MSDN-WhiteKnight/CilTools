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
    public class MethodDefSyntax : SyntaxNode
    {
        DirectiveSyntax _sig;
        BlockSyntax _body;

        public DirectiveSyntax Signature
        {
            get { return this._sig; }
        }

        public BlockSyntax Body
        {
            get { return this._body; }
        }

        internal MethodDefSyntax(DirectiveSyntax sig, BlockSyntax body)
        {
            this._sig = sig;
            this._body = body;
        }

        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");
            
            this._sig.ToText(target);
            this._body.ToText(target);
        }

        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            yield return this._sig;
            yield return this._body;
        }
    }
}
