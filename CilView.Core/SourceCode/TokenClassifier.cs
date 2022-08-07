﻿/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.SourceCode
{
    public abstract class TokenClassifier
    {
        public abstract SourceTokenKind GetKind(string token);

        protected static SourceTokenKind GetKindCommon(string token)
        {
            //common logic for C-like languages
            if (token.Length == 0) return SourceTokenKind.Unknown;

            if (char.IsDigit(token[0]))
            {
                return SourceTokenKind.NumericLiteral;
            }
            else if (token[0] == '"')
            {
                if (token.Length < 2 || token[token.Length - 1] != '"')
                {
                    return SourceTokenKind.Unknown;
                }
                else
                {
                    return SourceTokenKind.StringLiteral;
                }
            }
            else if (token[0] == '\'')
            {
                if (token.Length < 2 || token[token.Length - 1] != '\'')
                {
                    return SourceTokenKind.Unknown;
                }
                else
                {
                    return SourceTokenKind.StringLiteral;
                }
            }
            else if (token[0] == '/')
            {
                if (token.Length == 1)
                {
                    return SourceTokenKind.Punctuation;
                }
                else if (token[1] == '*')
                {
                    if (!token.EndsWith("*/", StringComparison.Ordinal))
                    {
                        return SourceTokenKind.Unknown;
                    }
                    else
                    {
                        return SourceTokenKind.Comment;
                    }
                }
                else if (token[1] == '/')
                {
                    return SourceTokenKind.Comment;
                }
                else
                {
                    return SourceTokenKind.Unknown;
                }
            }
            else if (char.IsPunctuation(token[0]) || char.IsSymbol(token[0]))
            {
                return SourceTokenKind.Punctuation;
            }
            else
            {
                return SourceTokenKind.Unknown;
            }
        }
    }
}
