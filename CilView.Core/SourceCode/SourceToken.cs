/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CilTools.Syntax;
using CilTools.Syntax.Tokens;

namespace CilView.SourceCode
{
    /// <summary>
    /// Represents a smallest lexical unit of a source text 
    /// (used for syntax highlighting in CIL View "Show source" feature)
    /// </summary>
    public class SourceToken : SyntaxNode
    {
        TokenKind _kind;
        string _content;
        
        public SourceToken(string content, TokenKind kind)
        {
            this._kind = kind;
            this._content = content;
            this._lead = string.Empty;
            this._trail = string.Empty;
        }

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

        public TokenKind Kind
        {
            get { return this._kind; }
        }

        public string Content
        {
            get { return this._content; }
        }
                
        public override void ToText(TextWriter target)
        {
            target.Write(this._lead);
            target.Write(this._content);
            target.Write(this._trail);
        }

        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            return new SyntaxNode[0];
        }
    }
}
