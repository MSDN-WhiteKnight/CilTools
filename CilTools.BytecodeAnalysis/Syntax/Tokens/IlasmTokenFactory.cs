/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Syntax.Tokens
{
    class IlasmTokenFactory : SyntaxFactory
    {
        private IlasmTokenFactory() { }

        public static readonly IlasmTokenFactory Value = new IlasmTokenFactory();

        public override SyntaxNode CreateNode(string content, string leadingWhitespace, string trailingWhitespace)
        {
            return SyntaxFactory.CreateFromToken(content, leadingWhitespace, trailingWhitespace);
        }
    }
}
