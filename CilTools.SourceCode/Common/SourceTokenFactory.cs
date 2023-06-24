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
    /// <summary>
    /// Provides a <see cref="SyntaxFactory"/> implementation that creates tokens for the specified source code language
    /// </summary>
    public class SourceTokenFactory : SyntaxFactory
    {
        TokenClassifier classifier;
        SourceLanguage language;

        internal SourceTokenFactory(TokenClassifier tc, SourceLanguage lang)
        {
            this.classifier = tc;
            this.language = lang;
        }

        /// <summary>
        /// Gets a programming language for this token factory
        /// </summary>
        public SourceLanguage Language
        {
            get { return this.language; }
        }

        /// <summary>
        /// Creates a new source token
        /// </summary>
        public override SyntaxNode CreateNode(string content, string leadingWhitespace, string trailingWhitespace)
        {
            SourceToken ret = SourceToken.CreateFromString(content, leadingWhitespace, trailingWhitespace, this.classifier);
            ret.Language = SourceCodeUtils.GetSourceLanguageName(this.language);
            return ret;
        }
    }
}
