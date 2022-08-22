/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CilTools.Syntax;
using CilView.Core.Syntax;

namespace CilView.SourceCode
{
    public enum SourceTokenKind
    {
        Unknown = 0,
        Keyword = 1,
        Punctuation,
        NumericLiteral,
        StringLiteral,
        Comment,
        TypeName,
        FunctionName,
        OtherName
    }

    public class SourceToken : SyntaxNode
    {
        SourceTokenKind _kind;
        string _content;
        
        public SourceToken(string content, SourceTokenKind kind)
        {
            this._kind = kind;
            this._content = content;
            this._lead = string.Empty;
            this._trail = string.Empty;
        }

        public SourceToken(string content, SourceTokenKind kind, string leadingWhitespace, string trailingWhitespace)
        {
            this._kind = kind;
            this._content = content;
            this._lead = leadingWhitespace;
            this._trail = trailingWhitespace;
        }
        
        static SourceToken CreateFromString(string tokenString, string leadingWhitespace, 
            string trailingWhitespace, TokenClassifier classifier)
        {
            if (leadingWhitespace == null) leadingWhitespace = string.Empty;
            if (trailingWhitespace == null) trailingWhitespace = string.Empty;

            SourceTokenKind kind = classifier.GetKind(tokenString);

            return new SourceToken(tokenString, kind, leadingWhitespace, trailingWhitespace);
        }

        public static SourceToken[] ParseTokens(string src, TokenClassifier classifier)
        {
            List<SourceToken> ret = new List<SourceToken>();
            TokenReader reader = new TokenReader(src);
            string[] tokens = reader.ReadAll().ToArray();

            if (tokens.Length == 0) return new SourceToken[0];

            string leadingWhitespace;
            int i = 0;

            if (SyntaxReader.IsWhitespace(tokens[0]))
            {
                leadingWhitespace = tokens[0];
                i = 1;
            }
            else
            {
                leadingWhitespace = string.Empty;
            }

            while (true)
            {
                if (i >= tokens.Length) break;

                if (i + 1 >= tokens.Length)
                {
                    ret.Add(CreateFromString(tokens[i], leadingWhitespace, string.Empty, classifier));
                    break;
                }
                else if (SyntaxReader.IsWhitespace(tokens[i + 1]))
                {
                    ret.Add(CreateFromString(tokens[i], leadingWhitespace, tokens[i + 1], classifier));
                    i += 2;
                }
                else
                {
                    ret.Add(CreateFromString(tokens[i], leadingWhitespace, string.Empty, classifier));
                    i++;
                }

                leadingWhitespace = string.Empty;
            }

            return ret.ToArray();
        }

        public SourceTokenKind Kind
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
