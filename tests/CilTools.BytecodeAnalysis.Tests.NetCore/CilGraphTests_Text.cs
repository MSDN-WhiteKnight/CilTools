/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;
using System.Text;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests.NetCore
{
    [TestClass]
    public class CilGraphTests_Text
    {
        [TestMethod]
        public void Test_CilGraph_ToString()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            CilGraphTestsCore_Text.Test_CilGraph_ToString(mi);
        }

        [TestMethod]
        public void Test_CilGraph_EmptyString()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("TestEmptyString");
            CilGraphTestsCore_Text.Test_CilGraph_EmptyString(mi);
        }

        [TestMethod]
        public void Test_CilGraph_OptionalParams()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("TestOptionalParams");
            CilGraphTestsCore_Text.Test_CilGraph_OptionalParams(mi);
        }

        [TestMethod]
        public void Test_CilGraph_ImplRuntime()
        {
            MethodInfo mi = typeof(System.Func<>).GetMethod("Invoke");
            CilGraphTestsCore_Text.Test_CilGraph_ImplRuntime(mi);
        }

#if DEBUG
        [TestMethod]
        public void Test_CilGraph_Locals()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("CreatePoint");
            CilGraphTestsCore_Text.Test_CilGraph_Locals(mi);
        }
#endif
    }
}
