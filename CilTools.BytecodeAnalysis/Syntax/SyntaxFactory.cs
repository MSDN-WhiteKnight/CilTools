﻿/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Syntax
{
    /// <summary>
    /// Provides static methods that create new instances of <see cref="SyntaxNode"/> class
    /// </summary>
    public static class SyntaxFactory
    {
        // Chars valid in DottedName token (ECMA-335 II.5.2 - Basic syntax categories)
        static readonly HashSet<char> validIdChars = new HashSet<char>(new char[] { '.', '_', '$', '@', '`', '?' });

        internal static string Strip(string input, int startOffset, int endOffset)
        {
            int len = input.Length - startOffset - endOffset;

            if (startOffset < 0) startOffset = 0;
            if (startOffset >= input.Length) startOffset = input.Length - 1;
            if (len < 0) len = 0;
            if (len > input.Length - startOffset) len = input.Length - startOffset;

            if (len == 0) return string.Empty;
            else return input.Substring(startOffset, len);
        }

        /// <summary>
        /// Creates a syntax node using the specified token string (for example, keyword or identifier)
        /// </summary>
        /// <param name="tokenString">String containing a single CIL assembler token</param>
        /// <param name="leadingWhitespace">
        /// String containing whitespace characters that precede the specified token in the source document
        /// </param>
        /// <param name="trailingWhitespace">
        /// String containing whitespace characters that follow the specified token in the source document
        /// </param>
        /// <remarks>
        /// <paramref name="tokenString"/> should be a valid token according to CIL assembler grammar. Leading and 
        /// trailing whitespace should be strings consisting of only whitespace characters (including CR and LF), empty 
        /// strings or null values. Otherwise, the behaviour is undefined.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Token string is null</exception>
        /// <exception cref="ArgumentException">Token string is empty</exception>
        public static SyntaxNode CreateFromToken(string tokenString, string leadingWhitespace, string trailingWhitespace)
        {
            if (tokenString == null) throw new ArgumentNullException("tokenString");

            if (tokenString.Length == 0)
            {
                throw new ArgumentException("Argument 'tokenString' should not be empty", "tokenString");
            }

            if (leadingWhitespace == null) leadingWhitespace = string.Empty;
            if (trailingWhitespace == null) trailingWhitespace = string.Empty;

            if (char.IsLetter(tokenString[0]) || validIdChars.Contains(tokenString[0]))
            {
                if (SyntaxClassifier.IsKeyword(tokenString))
                {
                    return new KeywordSyntax(leadingWhitespace, tokenString, trailingWhitespace,
                        SyntaxClassifier.ClassifyKeyword(tokenString));
                }
                else
                {
                    return new IdentifierSyntax(leadingWhitespace, tokenString, trailingWhitespace, false, null);
                }
            }
            else if (char.IsDigit(tokenString[0]))
            {
                return LiteralSyntax.CreateFromRawValue(leadingWhitespace, tokenString, trailingWhitespace);
            }
            else if (tokenString[0] == '"')
            {
                if (tokenString.Length < 2)
                {
                    return new InvalidSyntax(leadingWhitespace, tokenString,
                        "Token string is too short to be a valid string literal", trailingWhitespace);
                }

                if (tokenString[tokenString.Length - 1] != '"')
                {
                    return new InvalidSyntax(leadingWhitespace, tokenString,
                        "Token string is invalid: string literal does not have a closing quotation mark", 
                        trailingWhitespace);
                }

                return LiteralSyntax.CreateFromRawValue(leadingWhitespace, tokenString, trailingWhitespace);
            }
            else if (tokenString[0] == '\'')
            {
                if (tokenString.Length < 2)
                {
                    return new InvalidSyntax(leadingWhitespace, tokenString,
                        "Token string is too short to be a valid single-quoted literal", trailingWhitespace);
                }

                if (tokenString[tokenString.Length - 1] != '\'')
                {
                    return new InvalidSyntax(leadingWhitespace, tokenString,
                        "Token string is invalid: single-quoted literal does not have a closing quotation mark", 
                        trailingWhitespace);
                }

                return new IdentifierSyntax(leadingWhitespace, tokenString, trailingWhitespace, false, null);
            }
            else if (tokenString[0] == '/')
            {
                if (tokenString.Length == 1)
                {
                    return new PunctuationSyntax(leadingWhitespace, tokenString, trailingWhitespace);
                }
                else if (tokenString[1] == '*')
                {
                    if (!tokenString.EndsWith("*/", StringComparison.Ordinal))
                    {
                        return new InvalidSyntax(leadingWhitespace, tokenString,
                            "Token string is invalid: multiline comment does not have trailing */", trailingWhitespace);
                    }

                    return CommentSyntax.Create(leadingWhitespace, tokenString, trailingWhitespace, true);
                }
                else if (tokenString[1] == '/')
                {
                    return CommentSyntax.Create(leadingWhitespace, tokenString, trailingWhitespace, true);
                }
                else
                {
                    return new InvalidSyntax(leadingWhitespace, tokenString, "Unexpected token", trailingWhitespace);
                }
            }
            else if (char.IsPunctuation(tokenString[0]) || char.IsSymbol(tokenString[0]))
            {
                return new PunctuationSyntax(leadingWhitespace, tokenString, trailingWhitespace);
            }
            else
            {
                return new InvalidSyntax(leadingWhitespace, tokenString, "Unexpected token", trailingWhitespace);
            }
        }
    }
}
