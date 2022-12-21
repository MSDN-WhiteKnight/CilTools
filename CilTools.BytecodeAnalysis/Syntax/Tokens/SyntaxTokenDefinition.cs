/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Syntax.Tokens
{
    /// <summary>
    /// A base class for classes that define logic for reading specific kinds of tokens from a string
    /// </summary>
    public abstract class SyntaxTokenDefinition
    {
        static SyntaxTokenDefinition[] ilasmTokens = null;

        public static IEnumerable<SyntaxTokenDefinition> IlasmTokens
        {
            get
            {
                if (ilasmTokens == null)
                {
                    ilasmTokens = new SyntaxTokenDefinition[] {
                        new NameToken(), new PunctuationToken(), new WhitespaceToken(), new NumericLiteralToken(),
                        new DoubleQuotLiteralToken(), new SingleQuotLiteralToken(), new CommentToken(),
                        new MultilineCommentToken()
                    };
                }

                foreach (SyntaxTokenDefinition item in ilasmTokens) yield return item;
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

    /// <summary>
    /// Ilasm DottedName token (ECMA-335 II.5.2 - Basic syntax categories).
    /// </summary>
    public class NameToken : SyntaxTokenDefinition
    {
        public override TokenKind Kind => TokenKind.Name;

        static readonly HashSet<char> validChars = new HashSet<char>(new char[] { '.', '_', '$', '@', '`', '?' });

        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();

            return char.IsLetter(c) || validChars.Contains(c);
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            char c = reader.PeekChar();
            
            return char.IsLetterOrDigit(c) || validChars.Contains(c);
        }
    }

    public class PunctuationToken : SyntaxTokenDefinition
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

    /// <summary>
    /// Represents a whitespace that separates tokens.
    /// </summary>
    /// <remarks>
    /// Whitespaces are not actually tokens, but they are still included there as the tokenizer needs to preserve them 
    /// when separating tokens so later we could produce <see cref="SyntaxNode"/> instances with Leading/TrailingWhitespace
    /// properties set.
    /// </remarks>
    public class WhitespaceToken : SyntaxTokenDefinition
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

    public class NumericLiteralToken : SyntaxTokenDefinition
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

    public class DoubleQuotLiteralToken : SyntaxTokenDefinition
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

    public class SingleQuotLiteralToken : SyntaxTokenDefinition
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

    public class MultilineCommentToken : SyntaxTokenDefinition
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

    public class CommentToken : SyntaxTokenDefinition
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
