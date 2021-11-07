/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using CilTools.Reflection;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class ReflectionTests
    {
        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_NavigationExternal(MethodBase mi)
        {
            ReflectionTestsCore.Test_NavigationExternal(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "CreatePoint", BytecodeProviders.All)]
        public void Test_NavigationInternal(MethodBase mi)
        {
            ReflectionTestsCore.Test_NavigationInternal(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TypedRefTest", BytecodeProviders.All)]
        public void Test_TypedReferenceParam(MethodBase mi)
        {
            ReflectionTestsCore.Test_TypedReferenceParam(mi);
        }

        [TestMethod]        
        public void Test_GenericParamType()
        {
            GenericParamType tParam = GenericParamType.Create(typeof(IList<>), 0, null);
            Assert.IsTrue(tParam.IsGenericParameter);
            Assert.AreEqual("T", tParam.Name);
            Assert.AreEqual("IList`1", tParam.DeclaringType.Name);
            Assert.IsNull(tParam.DeclaringMethod);

            tParam = GenericParamType.Create(typeof(IList<>), 0, "T");
            Assert.IsTrue(tParam.IsGenericParameter);
            Assert.AreEqual("T", tParam.Name);
            Assert.AreEqual("IList`1", tParam.DeclaringType.Name);
            Assert.IsNull(tParam.DeclaringMethod);
        }
    }
}
