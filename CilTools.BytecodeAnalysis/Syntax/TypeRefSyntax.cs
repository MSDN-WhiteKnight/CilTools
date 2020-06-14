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
    public class TypeRefSyntax : SyntaxNode
    {
        SyntaxNode[] _content;

        internal TypeRefSyntax(SyntaxNode[] content)
        {
            this._content = content;
        }
        
        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            for (int i = 0; i < this._content.Length; i++) this._content[i].ToText(target);
            
            target.Flush();
        }

        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            for (int i = 0; i < this._content.Length; i++) yield return this._content[i]; 
        }
    }
}
