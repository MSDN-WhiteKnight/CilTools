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
    public class SignatureTests
    {
        [TestMethod]
        public void Test_Signature() 
        {
            byte[] data = new byte[] { 0x20, 0x0, 0x1 }; //instance void ()
            Signature sig=new Signature(data, typeof(SignatureTests).Module);
            Assert.IsTrue(sig.HasThis);
            Assert.IsFalse(sig.ExplicitThis);
            Assert.AreEqual(0, sig.ParamsCount);
            Assert.AreEqual(0, sig.GenericArgsCount);
            Assert.AreEqual("Void", sig.ReturnType.Name);
            Assert.AreEqual(ElementType.Void, sig.ReturnType.ElementType);
            Assert.AreEqual(CallingConvention.Default, sig.CallingConvention);
        }

        static readonly SignatureContext EmptyContext = SignatureContext.Create(
            MockTokenResolver.Value, GenericContext.Create(null, null), null);

        [TestMethod]
        public void Test_Signature_ReadFromArray_Void()
        {
            byte[] data = new byte[] { 0x0, 0x0, 0x1 }; //void ()            
            Signature sig = Signature.ReadFromArray(data, EmptyContext);
            Assert.IsFalse(sig.HasThis);
            Assert.IsFalse(sig.ExplicitThis);
            Assert.AreEqual(0, sig.ParamsCount);
            Assert.AreEqual("Void", sig.ReturnType.Name);
        }

        [TestMethod]
        public void Test_Signature_ReadFromArray_Numeric()
        {
            byte[] data = new byte[] { 0x20, 0x0, 0x8 }; //instance int32 ()
            Signature sig = Signature.ReadFromArray(data, EmptyContext);
            Assert.IsTrue(sig.HasThis);
            Assert.IsFalse(sig.ExplicitThis);
            Assert.AreEqual(0, sig.ParamsCount);
            Assert.AreEqual("Int32", sig.ReturnType.Name);
            Assert.IsTrue(sig.ReturnType.IsValueType);

            data = new byte[] { 0x0, 0x2, 0xd, 0xd, 0xd }; //float64 (float64,float64)
            sig = Signature.ReadFromArray(data, EmptyContext);
            Assert.AreEqual("Double", sig.ReturnType.Name);
            Assert.AreEqual(2, sig.ParamsCount);
            Assert.AreEqual("Double", sig.GetParamType(0).Name);
            Assert.AreEqual("Double", sig.GetParamType(1).Name);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "GenerateArray", BytecodeProviders.All)]
        public void Test_Signature_GenericMethodParam(MethodBase m)
        {
            byte[] data = new byte[] { 0x10, 0x1, 0x1, 0x1d, 0x1e, 0x0, 0x8 }; //!!0[] (int32)

            GenericContext gctx = GenericContext.Create(null, m);
            SignatureContext ctx = SignatureContext.Create(MockTokenResolver.Value, gctx, null);
            Signature sig = Signature.ReadFromArray(data, ctx);
            Assert.AreEqual(1, sig.ParamsCount);
            Assert.AreEqual("Int32", sig.GetParamType(0).Name);
            TypeSpec tRet = sig.ReturnType;
            TypeSpec tParam = tRet.InnerTypeSpec;
            Assert.IsTrue(tRet.IsArray);
            Assert.AreEqual(ElementType.SzArray, tRet.ElementType);
            Assert.IsTrue(tParam.IsGenericParameter);
            Assert.AreEqual(ElementType.MVar, tParam.ElementType);
            Assert.AreEqual(m.Name, tParam.DeclaringMethod.Name);
            Assert.AreEqual(0,tParam.GenericParameterPosition);
            Assert.AreEqual("T", tParam.Name);
        }

        [TestMethod]
        public void Test_Signature_GenericMethodParam_EmptyContext() 
        {
            byte[] data = new byte[] { 0x10, 0x1, 0x1, 0x1d, 0x1e, 0x0, 0x8 }; //!!0[] (int32)
            
            Signature sig = Signature.ReadFromArray(data, EmptyContext);
            Assert.AreEqual(1, sig.ParamsCount);
            Assert.AreEqual("Int32", sig.GetParamType(0).Name);
            TypeSpec tRet = sig.ReturnType;
            TypeSpec tParam = tRet.InnerTypeSpec;
            Assert.IsTrue(tRet.IsArray);
            Assert.AreEqual(ElementType.SzArray, tRet.ElementType);
            Assert.IsTrue(tParam.IsGenericParameter);
            Assert.AreEqual(ElementType.MVar, tParam.ElementType);
            Assert.AreEqual(0, tParam.GenericParameterPosition);
        }

        [TestMethod]
        public void Test_Signature_GenericTypeParam()
        {
            byte[] data = new byte[] { 0x10, 0x1, 0x1, 0x1d, 0x13, 0x0, 0x8 }; //!0[] (int32)

            GenericContext gctx = GenericContext.Create(typeof(List<>), null);
            SignatureContext ctx = SignatureContext.Create(MockTokenResolver.Value, gctx, null);
            Signature sig = Signature.ReadFromArray(data, ctx);
            TypeSpec tRet = sig.ReturnType;
            TypeSpec tParam = tRet.InnerTypeSpec;
            Assert.IsTrue(tRet.IsArray);
            Assert.AreEqual(ElementType.SzArray, tRet.ElementType);
            Assert.IsTrue(tParam.IsGenericParameter);
            Assert.AreEqual(ElementType.Var, tParam.ElementType);
            Assert.AreEqual(0, tParam.GenericParameterPosition);
            Assert.IsNull(tParam.DeclaringMethod);
            Assert.AreEqual("T", tParam.Name);
            Assert.AreEqual("List`1", tParam.DeclaringType.Name);

            sig = Signature.ReadFromArray(data, EmptyContext);
            tRet = sig.ReturnType;
            tParam = tRet.InnerTypeSpec;
            Assert.IsTrue(tRet.IsArray);
            Assert.AreEqual(ElementType.SzArray, tRet.ElementType);
            Assert.IsTrue(tParam.IsGenericParameter);
            Assert.IsNull(tParam.DeclaringMethod);
            Assert.AreEqual(ElementType.Var, tParam.ElementType);
            Assert.AreEqual(0, tParam.GenericParameterPosition);
        }
    }
}
