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

        static void VerifyGenericContext_IList(GenericContext ctx)
        {
            Assert.IsNull(ctx.DeclaringMethod);
            Assert.AreEqual("IList`1", ctx.DeclaringType.Name);
            Assert.AreEqual(0, ctx.MethodArgumentsCount);
            Assert.AreEqual(1, ctx.TypeArgumentsCount);
            Type t = ctx.GetTypeArgument(0);
            Assert.IsTrue(t.IsGenericParameter);
            Assert.AreEqual("T", t.Name);
        }

        [TestMethod]
        public void Test_GenericContext_GenericType()
        {
            GenericContext ctx = GenericContext.Create(typeof(IList<>), null);
            VerifyGenericContext_IList(ctx);

            ctx = GenericContext.Create(typeof(IList<>).GetGenericArguments(), null);
            VerifyGenericContext_IList(ctx);

            ctx = GenericContext.Create(new Type[] { typeof(int) }, null);
            Assert.IsNull(ctx.DeclaringMethod);
            Assert.AreEqual(0, ctx.MethodArgumentsCount);
            Assert.AreEqual(1, ctx.TypeArgumentsCount);
            Type t = ctx.GetTypeArgument(0);
            Assert.IsFalse(t.IsGenericParameter);
            Assert.AreEqual("Int32", t.Name);
        }

        static void VerifyGenericContext_Method(GenericContext ctx,MethodBase m)
        {
            Assert.AreEqual(1, ctx.MethodArgumentsCount);
            Assert.AreEqual(0, ctx.TypeArgumentsCount);
            Assert.IsNull(ctx.DeclaringType);
            Assert.AreEqual(m.Name, ctx.DeclaringMethod.Name);
            Type t = ctx.GetMethodArgument(0);
            Assert.IsTrue(t.IsGenericParameter);
            Assert.AreEqual("T", t.Name);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods),"GenerateArray",BytecodeProviders.All)]
        public void Test_GenericContext_GenericMethod(MethodBase m)
        {
            GenericContext ctx = GenericContext.Create(null, m);
            VerifyGenericContext_Method(ctx, m);

            ctx = GenericContext.Create(null, m.GetGenericArguments());
            VerifyGenericContext_Method(ctx, m);

            ctx = GenericContext.Create(null, new Type[] { typeof(int) });
            Assert.AreEqual(1, ctx.MethodArgumentsCount);
            Assert.AreEqual(0, ctx.TypeArgumentsCount);
            Assert.IsNull(ctx.DeclaringType);
            Assert.IsNotNull(ctx.DeclaringMethod);
            Type t = ctx.GetMethodArgument(0);
            Assert.IsFalse(t.IsGenericParameter);
            Assert.AreEqual("Int32", t.Name);
        }
    }
}
