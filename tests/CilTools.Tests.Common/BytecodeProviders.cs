/* CIL Tools tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;

namespace CilTools.Tests.Common
{
    /// <summary>
    /// Represents APIs that could be used to fetch method's bytecode from .NET assembly
    /// </summary>
    [Flags]
    public enum BytecodeProviders
    {
        /// <summary>
        /// Standard reflection
        /// </summary>
        Reflection = 0x01,

        /// <summary>
        /// CilTools.Metadata library
        /// </summary>
        Metadata = 0x02,

        /// <summary>
        /// All supported providers
        /// </summary>
        All = Reflection | Metadata
    }
}
