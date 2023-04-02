/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CilTools.Syntax;
using CilTools.Syntax.Tokens;

namespace CilTools.SourceCode.Common
{
    /// <summary>
    /// Represents a smallest lexical unit of a source text
    /// </summary>
    public class SourceToken : SyntaxNode //used for syntax highlighting in CIL View "Show source" feature
    {
        TokenKind _kind;
        string _content;
        
        /// <summary>
        /// Creates a new source token instance
        /// </summary>
        public SourceToken(string content, TokenKind kind)
        {
            this._kind = kind;
            this._content = content;
            this._lead = string.Empty;
            this._trail = string.Empty;
        }

        /// <summary>
        /// Creates a new source token instance with whitespace
        /// </summary>
        public SourceToken(string content, TokenKind kind, string leadingWhitespace, string trailingWhitespace)
        {
            this._kind = kind;
            this._content = content;
            this._lead = leadingWhitespace;
            this._trail = trailingWhitespace;
        }
        
        internal static SourceToken CreateFromString(string tokenString, string leadingWhitespace, 
            string trailingWhitespace, TokenClassifier classifier)
        {
            if (leadingWhitespace == null) leadingWhitespace = string.Empty;
            if (trailingWhitespace == null) trailingWhitespace = string.Empty;

            TokenKind kind = classifier.GetKind(tokenString);

            return new SourceToken(tokenString, kind, leadingWhitespace, trailingWhitespace);
        }

        /// <summary>
        /// Gets the token kind
        /// </summary>
        public TokenKind Kind
        {
            get { return this._kind; }
        }

        /// <summary>
        /// Gets the text content of this token without leading and trailing whitespace
        /// </summary>
        public string Content
        {
            get { return this._content; }
        }
        
        /// <inheritdoc/>
        public override void ToText(TextWriter target)
        {
            target.Write(this._lead);
            target.Write(this._content);
            target.Write(this._trail);
        }

        /// <inheritdoc/>
        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            return SyntaxNode.EmptyArray;
        }
    }
}
