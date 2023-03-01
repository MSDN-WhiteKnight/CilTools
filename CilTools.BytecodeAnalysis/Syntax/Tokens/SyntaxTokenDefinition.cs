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
    /// <remarks>
    /// Derived classes could use <see cref="TokenReader.PeekChar"/> to check the next character at the reader's 
    /// current position, but should not advance the reader's position by reading characters from it.
    /// </remarks>
    public abstract class SyntaxTokenDefinition
    {
        static SyntaxTokenDefinition[] ilasmTokens = null;

        /// <summary>
        /// Provides a collection of token definitions for the CIL assembler grammar 
        /// (ECMA-335 II.5.2 - Basic syntax categories).
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the current position of the <see cref="TokenReader"/> contains a sequence 
        /// of characters valid as the token start
        /// </summary>
        /// <param name="reader">A token reader to test</param>
        public abstract bool HasStart(TokenReader reader);

        /// <summary>
        /// Gets a value indicating whether the current position of the <see cref="TokenReader"/> contains a sequence 
        /// of characters valid as a continuation of the specified token
        /// </summary>
        /// <param name="prevPart">A part of token previously read from a token reader</param>
        /// <param name="reader">A token reader to test</param>
        public abstract bool HasContinuation(string prevPart, TokenReader reader);
    }

    /// <summary>
    /// Ilasm DottedName token (ECMA-335 II.5.3 - Identifiers).
    /// </summary>
    class NameToken : SyntaxTokenDefinition
    {
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

    /// <summary>
    /// Represents a token definition for a punctuation token
    /// </summary>
    public class PunctuationToken : SyntaxTokenDefinition
    {
        static bool IsPunctuation(char c)
        {
            return (char.IsPunctuation(c) || char.IsSymbol(c)) && c != '\'' && c != '"';
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
        /// <inheritdoc/>
        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();
            return char.IsWhiteSpace(c);
        }

        /// <inheritdoc/>
        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            char c = reader.PeekChar();
            return char.IsWhiteSpace(c);
        }
    }

    /// <summary>
    /// Represents a token definition for a numeric literal token (integer of floating point)
    /// </summary>
    public class NumericLiteralToken : SyntaxTokenDefinition
    {
        /// <inheritdoc/>
        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();

            return char.IsDigit(c);
        }

        /// <inheritdoc/>
        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            char c = reader.PeekChar();
            return char.IsDigit(c) || c == '.';
        }
    }

    /// <summary>
    /// Represents a token definition for double-quoted text literal (<c>"Hello, world"</c>)
    /// </summary>
    public class DoubleQuotLiteralToken : SyntaxTokenDefinition
    {
        /// <inheritdoc/>
        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();
            return c == '"';
        }

        /// <inheritdoc/>
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

    /// <summary>
    /// Represents a token definition for single-quoted text literal (<c>'x'</c>)
    /// </summary>
    public class SingleQuotLiteralToken : SyntaxTokenDefinition
    {
        /// <inheritdoc/>
        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();
            return c == '\'';
        }

        /// <inheritdoc/>
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

    /// <summary>
    /// Represents a token definition for a multiline comment (<c>/*Hello, world*/</c>)
    /// </summary>
    public class MultilineCommentToken : SyntaxTokenDefinition
    {
        /// <inheritdoc/>
        public override bool HasStart(TokenReader reader)
        {
            char[] chars = reader.PeekChars(2);
            if (chars.Length < 2) return false;

            return chars[0] == '/' && chars[1] == '*';
        }

        /// <inheritdoc/>
        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            if (prevPart.Length <= 2) return true;

            char c1 = prevPart[prevPart.Length - 2];
            char c2 = prevPart[prevPart.Length - 1];
            return !(c1 == '*' && c2 == '/');
        }
    }

    /// <summary>
    /// Represents a token definition for a single line comment (<c>// Hello, world</c>)
    /// </summary>
    public class CommentToken : SyntaxTokenDefinition
    {
        /// <inheritdoc/>
        public override bool HasStart(TokenReader reader)
        {
            char[] chars = reader.PeekChars(2);
            if (chars.Length < 2) return false;

            return chars[0] == '/' && chars[1] == '/';
        }

        /// <inheritdoc/>
        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            if (prevPart.Length < 2) return true;

            char c = reader.PeekChar();
            return !(c == '\n' || c == '\r');
        }
    }
}
