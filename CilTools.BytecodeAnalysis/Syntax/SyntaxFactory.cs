﻿/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Syntax
{
    public static class SyntaxFactory
    {
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

        public static SyntaxNode CreateFromToken(string tokenString, string leadingWhitespace, string trailingWhitespace)
        {
            if (tokenString == null) throw new ArgumentNullException("tokenString");

            if (tokenString.Length == 0)
            {
                throw new ArgumentException("Argument 'tokenString' should not be empty", "tokenString");
            }

            if (leadingWhitespace == null) leadingWhitespace = string.Empty;
            if (trailingWhitespace == null) trailingWhitespace = string.Empty;

            if (char.IsLetter(tokenString[0]) || tokenString[0] == '.' || tokenString[0] == '_')
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