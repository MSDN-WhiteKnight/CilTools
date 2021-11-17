/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;

namespace CilTools.BytecodeAnalysis
{
    /// <summary>
    /// Represents calling convention, a set of rules defining how the function interacts with its caller, as defined by ECMA-335
    /// </summary>
    public enum CallingConvention //ECMA-335 II.15.3: Calling convention 
    {
        /// <summary>
        /// Default managed calling convention (fixed amount of arguments)
        /// </summary>
        Default = 0x00,

        /// <summary>
        /// C language calling convention
        /// </summary>
        CDecl = 0x01,

        /// <summary>
        /// Standard x86 calling convention
        /// </summary>
        StdCall = 0x02,

        /// <summary>
        /// Calling convention for C++ class member functions
        /// </summary>
        ThisCall = 0x03,

        /// <summary>
        /// Optimized x86 calling convention
        /// </summary>
        FastCall = 0x04,

        /// <summary>
        /// Managed calling convention with variable amount of arguments
        /// </summary>
        Vararg = 0x05,
    }
}
