/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Syntax
{
    public abstract class SyntaxTreeNode
    {
        public abstract IEnumerable<SyntaxTreeNode> Children { get; }
        public abstract void ToText(TextWriter target);
    }
}
