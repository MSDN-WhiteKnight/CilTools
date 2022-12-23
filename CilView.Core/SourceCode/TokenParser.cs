/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CilTools.Syntax.Tokens;
using CilView.SourceCode.Common;
using CilView.SourceCode.VisualBasic;

namespace CilView.SourceCode
{
    public static class TokenParser
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

        public static SyntaxTokenDefinition[] GetDefinitions(string ext)
        {
            if (ext == null) ext = string.Empty;

            ext = ext.Trim();

            if (ext.Equals(".vb", StringComparison.OrdinalIgnoreCase))
            {
                return s_vbDefinitions;
            }
            else
            {
                return s_clikeDefinitions; //C-like
            }
        }

        static bool IsWhitespace(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (!char.IsWhiteSpace(str[i])) return false;
            }

            return true;
        }

        public static SourceToken[] ParseTokens(string src, SyntaxTokenDefinition[] definitions, 
            TokenClassifier classifier)
        {
            List<SourceToken> ret = new List<SourceToken>();
            TokenReader reader = new TokenReader(src, definitions);
            string[] tokens = reader.ReadAll().ToArray();

            if (tokens.Length == 0) return new SourceToken[0];

            string leadingWhitespace;
            int i = 0;

            if (IsWhitespace(tokens[0]))
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
                    ret.Add(SourceToken.CreateFromString(tokens[i], leadingWhitespace, string.Empty, classifier));
                    break;
                }
                else if (IsWhitespace(tokens[i + 1]))
                {
                    ret.Add(SourceToken.CreateFromString(tokens[i], leadingWhitespace, tokens[i + 1], classifier));
                    i += 2;
                }
                else
                {
                    ret.Add(SourceToken.CreateFromString(tokens[i], leadingWhitespace, string.Empty, classifier));
                    i++;
                }

                leadingWhitespace = string.Empty;
            }

            return ret.ToArray();
        }
    }
}
