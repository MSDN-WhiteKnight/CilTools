/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.BytecodeAnalysis;

namespace CilTools.Reflection
{
    internal interface ITypeInfo
    {
        IEnumerable<CustomModifier> Modifiers { get; }
        bool IsFunctionPointer();
        Signature TargetSignature { get; }
    }
}
