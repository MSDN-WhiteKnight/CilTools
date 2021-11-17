/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;

namespace CilTools.BytecodeAnalysis
{
    /// <summary>
    /// Represents signature element type as defined in ECMA-335
    /// </summary>
    public enum ElementType : byte //ECMA-335 II.23.1.16 Element types used in signatures
    {
        /// <summary>
        /// The absence of return value
        /// </summary>
        Void = 0x01,

        /// <summary>
        /// System.Boolean
        /// </summary>
        Boolean = 0x02,

        /// <summary>
        /// System.Char
        /// </summary>
        Char = 0x03,

        /// <summary>
        /// sbyte
        /// </summary>
        I1 = 0x04,

        /// <summary>
        /// byte
        /// </summary>
        U1 = 0x05,

        /// <summary>
        /// short
        /// </summary>
        I2 = 0x06,

        /// <summary>
        /// ushort
        /// </summary>
        U2 = 0x07,

        /// <summary>
        /// int
        /// </summary>
        I4 = 0x08,

        /// <summary>
        /// uint
        /// </summary>
        U4 = 0x09,

        /// <summary>
        /// long
        /// </summary>
        I8 = 0x0a,

        /// <summary>
        /// ulong
        /// </summary>
        U8 = 0x0b,

        /// <summary>
        /// float
        /// </summary>
        R4 = 0x0c,

        /// <summary>
        /// double
        /// </summary>
        R8 = 0x0d,

        /// <summary>
        /// Sytem.String
        /// </summary>
        String = 0x0e,

        /// <summary>
        /// Unmanaged pointer
        /// </summary>
        Ptr = 0x0f,  //Followed by type 

        /// <summary>
        /// Passed by reference
        /// </summary>
        ByRef = 0x10,  //Followed by type 

        /// <summary>
        /// Value type
        /// </summary>
        ValueType = 0x11,  //Followed by TypeDef or TypeRef token 

        /// <summary>
        /// Reference type
        /// </summary>
        Class = 0x12,  //Followed by TypeDef or TypeRef token 

        /// <summary>
        /// Generic parameter in a generic type definition
        /// </summary>
        Var = 0x13,  //represented as number (compressed unsigned integer) 

        /// <summary>
        /// Array
        /// </summary>
        Array = 0x14,  //type rank boundsCount bound1 … loCount lo1 … 

        /// <summary>
        /// Generic type instantiation
        /// </summary>
        GenericInst = 0x15,  //Followed by type type-arg-count  type-1 ... type-n 

        /// <summary>
        /// Passed by typed reference
        /// </summary>
        TypedByRef = 0x16,

        /// <summary>
        /// System.IntPtr
        /// </summary>
        I = 0x18,

        /// <summary>
        /// System.UIntPtr
        /// </summary>
        U = 0x19,

        /// <summary>
        /// Function pointer
        /// </summary>
        FnPtr = 0x1b,  //Followed by full method signature 

        /// <summary>
        /// System.Object
        /// </summary>
        Object = 0x1c,

        /// <summary>
        /// Single-dimensional array with 0 lower bound
        /// </summary>
        SzArray = 0x1d,

        /// <summary>
        /// Generic parameter in a generic method definition
        /// </summary>
        MVar = 0x1e, //Represented as number (compressed unsigned integer)

        /// <summary>
        /// Implemented within the CLI
        /// </summary>
        Internal = 0x21,

        /// <summary>
        /// Modifier
        /// </summary>
        Modifier = 0x40,

        /// <summary>
        /// Sentinel for vararg method signature
        /// </summary>
        Sentinel = 0x41
    }
}
