/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;

namespace CilView.Core.Syntax
{
    public enum TokenKind
    {
        Unknown = 0,
        Name = 1,
        TypeName,
        FunctionName,
        Keyword,
        Punctuation,
        SingleQuotLiteral,
        DoubleQuotLiteral,
        NumericLiteral,
        Comment,
        MultilineComment,
        Whitespace
    }

    // Keyword, TypeName and FunctionName tokens are represented as Name at token parsing stage.
    // They could only be distinguished subsequently on semantic processing / classification.
}
