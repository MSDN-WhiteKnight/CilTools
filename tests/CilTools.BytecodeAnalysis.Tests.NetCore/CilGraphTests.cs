/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests.NetCore
{
    [TestClass]
    public class CilGraphTests
    {
        [TestMethod]
        public void Test_CilGraph_HelloWorld()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            CilGraphTestsCore.Test_CilGraph_HelloWorld(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Loop()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintTenNumbers");
            CilGraphTestsCore.Test_CilGraph_Loop(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Exceptions()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("DivideNumbers");
            CilGraphTestsCore.Test_CilGraph_Exceptions(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Tokens()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("TestTokens");
            CilGraphTestsCore.Test_CilGraph_Tokens(mi);
        }

#if DEBUG
        [TestMethod]
        public void Test_CilGraph_Pointer()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PointerTest");
            CilGraphTestsCore.Test_CilGraph_Pointer(mi);
        }
#endif



    }
}
