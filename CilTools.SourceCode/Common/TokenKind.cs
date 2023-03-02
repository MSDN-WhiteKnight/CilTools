/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;

namespace CilTools.SourceCode.Common
{
    /// <summary>
    /// Represents the kind of token, a lexical element in the formal language grammar.
    /// </summary>
    public enum TokenKind
    {
        /// <summary>
        /// Unknown token
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Uncategorized identifier token (keyword or name of function/type/variable etc.)
        /// </summary>
        Name = 1,

        /// <summary>
        /// Identifier token that represents a type name
        /// </summary>
        TypeName,

        /// <summary>
        /// Identifier token that represents a function name
        /// </summary>
        FunctionName,

        /// <summary>
        /// Keyword token
        /// </summary>
        Keyword,

        /// <summary>
        /// Punctuation sign token
        /// </summary>
        Punctuation,

        /// <summary>
        /// Single-quoted text literal token ('foo')
        /// </summary>
        SingleQuotLiteral,

        /// <summary>
        /// Double-quoted text literal token ("foo")
        /// </summary>
        DoubleQuotLiteral,

        /// <summary>
        /// Text literal with a special syntax, such as verbatim string literal in C#
        /// </summary>
        SpecialTextLiteral,

        /// <summary>
        /// Literal token of numeric type (integer or floating-point)
        /// </summary>
        NumericLiteral,

        /// <summary>
        /// Single-line comment token
        /// </summary>
        Comment,

        /// <summary>
        /// Multiline comment token
        /// </summary>
        MultilineComment
    }

    // Keyword, TypeName and FunctionName tokens are represented as Name at token parsing stage.
    // They could only be distinguished subsequently on semantic processing / classification.
}
