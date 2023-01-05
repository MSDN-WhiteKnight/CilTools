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
    public class SourceTokenFactory : SyntaxFactory
    {
        TokenClassifier classifier;
        SourceLanguage language;

        internal SourceTokenFactory(TokenClassifier tc, SourceLanguage lang)
        {
            this.classifier = tc;
            this.language = lang;
        }

        public SourceLanguage Language
        {
            get { return this.language; }
        }

        public override SyntaxNode CreateNode(string content, string leadingWhitespace, string trailingWhitespace)
        {
            return SourceToken.CreateFromString(content, leadingWhitespace, trailingWhitespace, this.classifier);
        }
    }
}
