/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.Syntax;
using CilTools.Syntax.Tokens;

namespace CilTools.SourceCode.Common
{
    /// <summary>
    /// Defines a name (identifier or keyword) token common to multiple C-like programming languages
    /// </summary>
    internal class CommonNameToken : SyntaxTokenDefinition
    {
        public override TokenKind Kind => TokenKind.Name;

        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();

            return char.IsLetter(c) || c == '_';
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            char c = reader.PeekChar();
            return char.IsLetterOrDigit(c) || c == '_';
        }
    }
}
