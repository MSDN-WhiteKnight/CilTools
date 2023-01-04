/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.Syntax;
using CilTools.Syntax.Tokens;

namespace CilTools.SourceCode.Common
{
    class SourceTokenFactory : SyntaxFactory
    {
        TokenClassifier classifier;

        public SourceTokenFactory(TokenClassifier tc)
        {
            this.classifier = tc;
        }

        public override SyntaxNode CreateNode(string content, string leadingWhitespace, string trailingWhitespace)
        {
            return SourceToken.CreateFromString(content, leadingWhitespace, trailingWhitespace, this.classifier);
        }
    }
}
