/* CilTools.BytecodeAnalysis library
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Reflection
{
    public interface ICustomAttribute
    {
        MethodBase Owner { get; }
        MethodBase Constructor { get; }
        byte[] Data { get; }
    }
}
