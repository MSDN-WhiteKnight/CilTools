/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.Syntax;
using CilTools.Syntax.Tokens;

namespace CilTools.SourceCode.VisualBasic
{
    internal class VbCommentToken : SyntaxTokenDefinition
    {
        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();
            return c == '\'';
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            if (prevPart.Length < 2) return true;

            char c = reader.PeekChar();
            return !(c == '\n' || c == '\r');
        }
    }
}
