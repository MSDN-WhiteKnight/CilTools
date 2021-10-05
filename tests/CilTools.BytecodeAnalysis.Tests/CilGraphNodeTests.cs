/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class CilGraphNodeTests
    {
        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "DivideNumbers", BytecodeProviders.All)]
        public void Test_CilGraphNode_Exceptions(MethodBase mi)
        {
            CilGraphNodeTestsCore.Test_CilGraphNode_Exceptions(mi);
        }
    }
}
