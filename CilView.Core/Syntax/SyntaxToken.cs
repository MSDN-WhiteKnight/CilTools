/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.Core.Syntax
{
    internal enum TokenKind
    {
        Unknown = 0, 
        Name = 1, 
        Punctuation, 
        SingleQuotLiteral, 
        DoubleQuotLiteral, 
        NumericLiteral, 
        Comment,
        MultilineComment,
        Whitespace
    }

    internal abstract class SyntaxToken
    {
        static SyntaxToken[] tokens = null;

        public static SyntaxToken[] AllTokens
        {
            get
            {
                if (tokens == null)
                {
                    tokens = new SyntaxToken[] {
                        new NameToken(), new PunctuationToken(), new WhitespaceToken(), new NumericLiteralToken(),
                        new DoubleQuotLiteralToken(), new SingleQuotLiteralToken(), new CommentToken(),
                        new MultilineCommentToken()
                    };
                }

                return tokens;
            }
        }

        internal static bool IsEscaped(string str, int i)
        {
            if (i <= 0) return false;

            char c1 = str[i - 1];

            if (c1 != '\\') return false;

            //check if the slash itself not escaped
            return !IsEscaped(str, i - 1);
        }

        public abstract TokenKind Kind { get; }
        public abstract bool HasStart(TokenReader reader);
        public abstract bool HasContinuation(string prevPart, TokenReader reader);
    }

    internal class NameToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.Name;

        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();

            return char.IsLetter(c) || c == '.' || c == '_';
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            char c = reader.PeekChar();
            return char.IsLetterOrDigit(c) || c == '.' || c == '_';
        }
    }

    internal class PunctuationToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.Punctuation;

        static bool IsPunctuation(char c)
        {
            return (char.IsPunctuation(c) || char.IsSymbol(c)) && c != '\'' && c != '"';
        }

        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();

            if (c == '/')
            {
                char[] chars = reader.PeekChars(2);
                if (chars.Length >= 2 && (chars[1] == '*' || chars[1] == '/')) return false;
                else return true;
            }
            else
            {
                return IsPunctuation(c);
            }
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            if (prevPart.Length >= 1) return false;
            else return true;
        }
    }

    internal class WhitespaceToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.Whitespace;

        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();
            return char.IsWhiteSpace(c);
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            char c = reader.PeekChar();
            return char.IsWhiteSpace(c);
        }
    }

    internal class NumericLiteralToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.NumericLiteral;

        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();

            return char.IsDigit(c);
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            char c = reader.PeekChar();
            return char.IsDigit(c) || c == '.';
        }
    }

    internal class DoubleQuotLiteralToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.DoubleQuotLiteral;

        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();
            return c == '"';
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            if (prevPart.Length <= 1) return true;

            char c = prevPart[prevPart.Length - 1];

            if (c == '"')
            {
                if (IsEscaped(prevPart, prevPart.Length - 1)) return true;
                else return false;
            }
            else return true;
        }
    }

    internal class SingleQuotLiteralToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.SingleQuotLiteral;

        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();
            return c == '\'';
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            if (prevPart.Length <= 1) return true;

            char c = prevPart[prevPart.Length - 1];

            if (c == '\'')
            {
                if (IsEscaped(prevPart, prevPart.Length - 1)) return true;
                else return false;
            }
            else return true;
        }
    }

    internal class MultilineCommentToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.MultilineComment;

        public override bool HasStart(TokenReader reader)
        {
            /**/
            char[] chars = reader.PeekChars(2);
            if (chars.Length < 2) return false;

            return chars[0] == '/' && chars[1] == '*';
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            if (prevPart.Length <= 2) return true;

            char c1 = prevPart[prevPart.Length - 2];
            char c2 = prevPart[prevPart.Length - 1];
            return !(c1 == '*' && c2 == '/');
        }
    }

    internal class CommentToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.MultilineComment;

        public override bool HasStart(TokenReader reader)
        {
            //
            char[] chars = reader.PeekChars(2);
            if (chars.Length < 2) return false;

            return chars[0] == '/' && chars[1] == '/';
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            if (prevPart.Length < 2) return true;

            char c = reader.PeekChar();
            return !(c == '\n' || c == '\r');
        }
    }
}
