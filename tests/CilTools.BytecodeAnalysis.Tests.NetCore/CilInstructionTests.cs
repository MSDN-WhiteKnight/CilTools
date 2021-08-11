/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests.NetCore
{
    [TestClass]
    public class CilInstructionTests
    {
        [TestMethod]
        public void Test_CilInstruction_Roundtrip()
        {
            CilInstructionTestsCore.Test_CilInstruction_Roundtrip();
        }

        [TestMethod]
        public void Test_CilInstruction_ToString()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            CilInstructionTestsCore.Test_CilInstruction_ToString(mi);
        }
    }
}