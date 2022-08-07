/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

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

    public class SourceToken
    {
        SourceTokenKind _kind;
        string _content;
        string _lead;
        string _trail;

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

        public SourceTokenKind Kind
        {
            get { return this._kind; }
        }

        public string Content
        {
            get { return this._content; }
        }

        public string LeadingWhitespace
        {
            get { return this._lead; }
        }

        public string TrailingWhitespace
        {
            get { return this._trail; }
        }

        public override string ToString()
        {
            return this._lead + this._content + this._trail;
        }
    }
}
