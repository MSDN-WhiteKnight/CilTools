/* CIL Tools 
 * Copyright (c) 2023, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.SourceCode.CSharp;
using CilTools.SourceCode.Cpp;
using CilTools.SourceCode.VisualBasic;
using CilTools.Syntax;
using CilTools.Syntax.Tokens;

namespace CilTools.SourceCode.Common
{
    public static class SourceCodeUtils
    {
        static readonly SyntaxTokenDefinition[] s_vbDefinitions = new SyntaxTokenDefinition[] {
            new CommonNameToken(), new PunctuationToken(), new WhitespaceToken(), new NumericLiteralToken(),
            new DoubleQuotLiteralToken(), new VbCommentToken()
        };

        static readonly SyntaxTokenDefinition[] s_clikeDefinitions = new SyntaxTokenDefinition[] {
            new CommonNameToken(), new PunctuationToken(), new WhitespaceToken(), new NumericLiteralToken(),
            new DoubleQuotLiteralToken(), new SingleQuotLiteralToken(), new CommentToken(),
            new MultilineCommentToken()
        };

        public static IEnumerable<SyntaxTokenDefinition> GetTokenDefinitions(string ext)
        {
            if (ext == null) ext = string.Empty;

            ext = ext.Trim();
            SyntaxTokenDefinition[] ret;

            if (ext.Equals(".vb", StringComparison.OrdinalIgnoreCase))
            {
                ret = s_vbDefinitions;
            }
            else
            {
                ret = s_clikeDefinitions; //C-like
            }

            foreach (SyntaxTokenDefinition item in ret) yield return item;
        }

        static bool IsCppExtension(string ext)
        {
            return ext == ".cpp" || ext == ".c" || ext == ".h" || ext == string.Empty;
        }

        public static TokenClassifier CreateClassifier(string ext)
        {
            if (ext == null) ext = string.Empty;

            ext = ext.Trim();

            if (IsCppExtension(ext))
            {
                return new CppClassifier();
            }
            else if (ext.Equals(".vb", StringComparison.OrdinalIgnoreCase))
            {
                return new VbClassifier();
            }
            else
            {
                return new CsharpClassifier();
            }
        }

        internal static TokenKind GetKindCommon(string token)
        {
            //common logic for C-like languages
            if (token.Length == 0) return TokenKind.Unknown;

            if (char.IsDigit(token[0]))
            {
                return TokenKind.NumericLiteral;
            }
            else if (token[0] == '"')
            {
                if (token.Length < 2 || token[token.Length - 1] != '"')
                {
                    return TokenKind.Unknown;
                }
                else
                {
                    return TokenKind.DoubleQuotLiteral;
                }
            }
            else if (token[0] == '\'')
            {
                if (token.Length < 2 || token[token.Length - 1] != '\'')
                {
                    return TokenKind.Unknown;
                }
                else
                {
                    return TokenKind.SingleQuotLiteral;
                }
            }
            else if (token[0] == '/')
            {
                if (token.Length == 1)
                {
                    return TokenKind.Punctuation;
                }
                else if (token[1] == '*')
                {
                    if (!token.EndsWith("*/", StringComparison.Ordinal))
                    {
                        return TokenKind.Unknown;
                    }
                    else
                    {
                        return TokenKind.MultilineComment;
                    }
                }
                else if (token[1] == '/')
                {
                    return TokenKind.Comment;
                }
                else
                {
                    return TokenKind.Unknown;
                }
            }
            else if (char.IsPunctuation(token[0]) || char.IsSymbol(token[0]))
            {
                return TokenKind.Punctuation;
            }
            else
            {
                return TokenKind.Unknown;
            }
        }
    }
}
