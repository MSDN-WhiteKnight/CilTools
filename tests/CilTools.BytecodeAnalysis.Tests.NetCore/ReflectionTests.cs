/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests.NetCore
{
    [TestClass]
    public class ReflectionTests
    {
        [TestMethod]
        public void Test_NavigationExternal()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            ReflectionTestsCore.Test_NavigationExternal(mi);
        }

        [TestMethod]
        public void Test_NavigationInternal()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("CreatePoint");
            ReflectionTestsCore.Test_NavigationInternal(mi);
        }

        [TestMethod]
        public void Test_TypedReferenceParam()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("TypedRefTest");
            ReflectionTestsCore.Test_TypedReferenceParam(mi);
        }
    }
}
