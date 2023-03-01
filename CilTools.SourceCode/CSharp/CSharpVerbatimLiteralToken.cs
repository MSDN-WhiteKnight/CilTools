/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.Syntax;
using CilTools.Syntax.Tokens;

namespace CilTools.SourceCode.CSharp
{
    /// <summary>
    /// Represents a token definition for verbatim string literal (<c>@"Hello, world"</c>)
    /// </summary>
    class CSharpVerbatimLiteralToken : SyntaxTokenDefinition
    {
        /// <inheritdoc/>
        public override bool HasStart(TokenReader reader)
        {
            string s = reader.PeekString(2);

            return string.Equals(s, "@\"", StringComparison.Ordinal);
        }

        static int GetCountOfTrailingQuotes(string s)
        {
            if (s.Length == 0) return 0;

            int c = 0;

            for (int i = s.Length - 1; i >= 2; i--)
            {
                if (s[i] == '"') c++;
                else break;
            }

            return c;
        }

        /// <inheritdoc/>
        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            if (prevPart.Length <= 2) return true;

            char c = prevPart[prevPart.Length - 1];

            if (c != '"') return true;

            char next = reader.PeekChar();
            
            if (next == '"') return true;

            int n = GetCountOfTrailingQuotes(prevPart);

            //verbatim literal string ends by odd number of quotes
            return (n % 2) == 0;
        }
    }
}
