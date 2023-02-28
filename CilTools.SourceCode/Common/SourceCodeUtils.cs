/* CIL Tools 
 * Copyright (c) 2023, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
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

        static readonly SyntaxTokenDefinition[] s_csharpDefinitions = new SyntaxTokenDefinition[] {
            new CommonNameToken(), new CSharpVerbatimLiteralToken(), new PunctuationToken(), new WhitespaceToken(), 
            new NumericLiteralToken(), new DoubleQuotLiteralToken(), new SingleQuotLiteralToken(), new CommentToken(),
            new MultilineCommentToken()
        };

        static readonly SourceTokenFactory s_csFactory = new SourceTokenFactory(
            new CSharpClassifier(), SourceLanguage.CSharp);

        static readonly SourceTokenFactory s_cppFactory = new SourceTokenFactory(
            new CppClassifier(), SourceLanguage.Cpp);

        static readonly SourceTokenFactory s_vbFactory = new SourceTokenFactory(
            new VbClassifier(), SourceLanguage.VisualBasic);

        public static IEnumerable<SyntaxTokenDefinition> GetTokenDefinitions(string ext)
        {
            if (ext == null) ext = string.Empty;

            ext = ext.Trim();
            SyntaxTokenDefinition[] ret;

            if (ext.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                ret = s_csharpDefinitions;
            }
            else if (ext.Equals(".vb", StringComparison.OrdinalIgnoreCase))
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

        public static SourceTokenFactory GetFactory(string ext)
        {
            if (ext == null) ext = string.Empty;

            ext = ext.Trim();

            if (IsCppExtension(ext))
            {
                return s_cppFactory;
            }
            else if (ext.Equals(".vb", StringComparison.OrdinalIgnoreCase))
            {
                return s_vbFactory;
            }
            else
            {
                return s_csFactory;
            }
        }

        public static SourceTokenFactory GetFactory(SourceLanguage lang)
        {
            switch (lang)
            {
                case SourceLanguage.CSharp: return s_csFactory;
                case SourceLanguage.Cpp: return s_cppFactory;
                case SourceLanguage.VisualBasic: return s_vbFactory;
                default:throw new ArgumentException("Unknown source language: " + lang.ToString());
            }
        }

        public static SourceToken[] ReadAllTokens(string src, IEnumerable<SyntaxTokenDefinition> definitions,
            SourceTokenFactory factory)
        {
            return SyntaxReader.ReadAllNodes(src, definitions, factory).Cast<SourceToken>().ToArray();
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
