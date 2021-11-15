/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Reflection;
using CilTools.Tests.Common;

namespace CilTools.BytecodeAnalysis.Tests.Signatures
{
    [TestClass]
    public class TypeSpecTests
    {
        [TestMethod]
        public void Test_ReadFromArray_Int()
        {
            byte[] data = new byte[] { 0x08 };
            GenericContext gctx = GenericContext.Create((Type)null, null);
            SignatureContext ctx = new SignatureContext(MockTokenResolver.Value, gctx);
            TypeSpec ts = TypeSpec.ReadFromArray(data, ctx);
            Assert.AreEqual(ElementType.I4, ts.ElementType);
            Assert.IsFalse(ts.IsPinned);
            Assert.IsFalse(ts.IsByRef);
            Assert.IsFalse(ts.IsArray);
            Assert.IsFalse(ts.IsPointer);
            Assert.IsFalse(ts.IsGenericType);
            Assert.IsTrue(ts.IsValueType);
            Assert.IsTrue(ts.IsPublic);
            Assert.AreEqual(0, ts.ModifiersCount);
            Assert.AreEqual("System.Int32", ts.FullName);
        }

        [TestMethod]        
        public void Test_ReadFromArray_GenericTypeParam()
        {
            byte[] data = new byte[] { 0x13, 0x0 };
            GenericContext gctx = GenericContext.Create(typeof(IList<>), null);
            SignatureContext ctx = new SignatureContext(MockTokenResolver.Value, gctx);
            TypeSpec ts = TypeSpec.ReadFromArray(data, ctx);
            Assert.IsTrue(ts.IsGenericParameter);
            Assert.AreEqual("T", ts.Name);
            Assert.AreEqual("IList`1", ts.DeclaringType.Name);
            Assert.IsNull(ts.DeclaringMethod);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "GenerateArray", BytecodeProviders.All)]
        public void Test_ReadFromArray_GenericMethodParam(MethodBase m)
        {
            byte[] data = new byte[] { 0x1e, 0x0 };
            GenericContext gctx = GenericContext.Create(null, m);
            SignatureContext ctx = new SignatureContext(MockTokenResolver.Value, gctx);
            TypeSpec ts = TypeSpec.ReadFromArray(data, ctx);
            Assert.IsTrue(ts.IsGenericParameter);
            Assert.AreEqual("T", ts.Name);
            Assert.AreEqual(m.Name, ts.DeclaringMethod.Name);
        }
    }
}
