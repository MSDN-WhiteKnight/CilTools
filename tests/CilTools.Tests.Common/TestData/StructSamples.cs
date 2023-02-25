/* CIL Tools
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Runtime.InteropServices;

namespace CilTools.Tests.Common.TestData
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 8)]
    public struct ExplicitStructSample
    {
        [FieldOffset(1)]
        public int x;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2, Size = 4)]
    public struct SequentialStructSample
    {        
        public int x;
    }
}
