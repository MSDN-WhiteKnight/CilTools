/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilTools.Syntax
{
    public static class SyntaxFactory
    {
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
                    return new KeywordSyntax(leadingWhitespace, tokenString, trailingWhitespace, KeywordKind.Other);
                }
                else
                {
                    return new IdentifierSyntax(leadingWhitespace, tokenString, trailingWhitespace, false, null);
                }
            }
            else if (char.IsDigit(tokenString[0]))
            {
                int i;

                if (int.TryParse(tokenString, out i))
                {
                    return new LiteralSyntax(leadingWhitespace, i, trailingWhitespace);
                }
                else
                {
                    return new GenericSyntax(leadingWhitespace + tokenString + trailingWhitespace);
                }
            }
            else
            {
                return new GenericSyntax(leadingWhitespace + tokenString + trailingWhitespace);
            }
        }
    }
}
