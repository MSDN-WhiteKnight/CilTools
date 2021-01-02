/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests.NetCore
{
    [TestClass]
    public class CilAnalysisTests
    {
        [TestMethod]
        public void Test_GetReferencedMethods()
        {
            CilAnalysisTestsCore.Test_GetReferencedMethods(typeof(SampleMethods).GetMethod("PrintProcessId"));
        }

        [TestMethod]
        public void Test_GetReferencedMembers()
        {
            CilAnalysisTestsCore.Test_GetReferencedMembers(typeof(SampleMethods).GetMethod("SquareFoo"));
        }
    }
}
