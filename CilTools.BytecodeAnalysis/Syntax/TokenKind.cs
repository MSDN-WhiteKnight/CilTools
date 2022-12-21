/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;

namespace CilTools.Syntax
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
        /// Literal token of numeric type (integer or floating-point)
        /// </summary>
        NumericLiteral,

        /// <summary>
        /// Single-line comment token (//...)
        /// </summary>
        Comment,

        /// <summary>
        /// Multiline comment token (/*...*/)
        /// </summary>
        MultilineComment,

        /// <summary>
        /// <para>Whitespace sequence that separates tokens.</para>
        /// <para>Whitespaces are not actually tokens, but they are still included there as the tokenizer needs to preserve them 
        /// when separating tokens so later we could produce <see cref="SyntaxNode"/> instances with Leading/TrailingWhitespace
        /// properties set.</para>
        /// </summary>
        Whitespace
    }

    // Keyword, TypeName and FunctionName tokens are represented as Name at token parsing stage.
    // They could only be distinguished subsequently on semantic processing / classification.
}
