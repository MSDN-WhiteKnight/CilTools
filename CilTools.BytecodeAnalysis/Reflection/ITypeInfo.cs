/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.BytecodeAnalysis;

namespace CilTools.Reflection
{
    /// <summary>
    /// Represents type information not covered by standard reflection.
    /// </summary>
    public interface ITypeInfo
    {
        /// <summary>
        /// Enumerates custom modifiers associated with the type
        /// </summary>
        IEnumerable<CustomModifier> Modifiers { get; }

        /// <summary>
        /// Gets a value indicating whether the type represents a function pointer
        /// </summary>
        bool IsFunctionPointer { get; }

        /// <summary>
        /// Gets the target function signature, if the type represents a function pointer. Otherwise, returns <c>null</c>.
        /// </summary>
        Signature TargetSignature { get; }
    }
}
