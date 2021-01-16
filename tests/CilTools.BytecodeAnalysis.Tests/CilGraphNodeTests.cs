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
        public void Test_CilGraphNode_Exceptions()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("DivideNumbers");
            CilGraphNodeTestsCore.Test_CilGraphNode_Exceptions(mi);
        }
    }
}
