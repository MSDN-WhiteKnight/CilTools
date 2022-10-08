/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;

namespace CilTools.Reflection
{
    /// <summary>
    /// Represents the resolution mode for external member references
    /// </summary>
    public enum RefResolutionMode
    {
        /// <summary>
        /// Specifies that external references should not be resolved.
        /// </summary>
        NoResolve = 1,

        /// <summary>
        /// Specifies that external references should be resolved and exception should be thrown if the resolution failed.
        /// </summary>
        RequireResolve = 2,

        /// <summary>
        /// Specifies that external references should be resolved, but if the resolution failed the API should fallback 
        /// to implementation that does not need a resolved reference.
        /// </summary>
        TryResolve = 3
    }
}
