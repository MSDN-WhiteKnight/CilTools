/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;

namespace CilTools.Syntax.Tokens
{
    /// <summary>
    /// Provides a base class for classes that define logic for determining the <see cref="TokenKind"/> of a given token
    /// </summary>
    /// <remarks>
    /// Token classifiers are used mainly to enable syntax highlighting in "Show source" UI. Classes derived from this class 
    /// define the classification logic specific to a given programming language.
    /// </remarks>
    public abstract class TokenClassifier
    {
        /// <summary>
        /// Gets a token kind for the specified token string
        /// </summary>
        /// <param name="token">Token string to classify</param>
        /// <returns>Token kind of the classified token</returns>
        public abstract TokenKind GetKind(string token);
    }
}
