/* CIL Tools 
 * Copyright (c) 2023, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CilTools.Syntax;

namespace CilTools.SourceCode.Common
{
    /// <summary>
    /// Represents a smallest lexical unit of a source text
    /// </summary>
    public class SourceToken : SyntaxNode //used for syntax highlighting in CIL View "Show source" feature
    {
        TokenKind _kind;
        string _content;
        string _lang;
        int _ordinal;
        
        /// <summary>
        /// Creates a new source token instance
        /// </summary>
        public SourceToken(string content, TokenKind kind)
        {
            this._kind = kind;
            this._content = content;
            this._lead = string.Empty;
            this._trail = string.Empty;
            this._lang = string.Empty;
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
            this._lang = string.Empty;
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

        /// <summary>
        /// Gets or sets the programming language name for the document this token is included in
        /// </summary>
        public string Language
        {
            get { return this._lang; }
            set { this._lang = value; }
        }

        /// <summary>
        /// Gets or sets the ordinal number of this token in a sequence of tokens. The value is undefined if this token is not 
        /// included in a sequence of tokens.
        /// </summary>
        /// <remarks>
        /// This property could be useful to find previous/next token in a sequence of tokens parsed from a source text
        /// </remarks>
        public int OrdinalNumber
        {
            get { return this._ordinal; }
            set { this._ordinal = value; }
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
