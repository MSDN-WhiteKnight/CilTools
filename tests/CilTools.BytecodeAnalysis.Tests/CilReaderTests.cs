/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class CilReaderTests
    {
        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", MethodSource.All)]
        public void Test_CilReader_HelloWorld(MethodBase mi)
        {
            CilReaderTestsCore.Test_CilReader_HelloWorld(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "CalcSum", MethodSource.All)]
        public void Test_CilReader_CalcSum(MethodBase mi)
        {
            CilReaderTestsCore.Test_CilReader_CalcSum(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "SquareFoo", MethodSource.All)]
        public void Test_CilReader_StaticFieldAccess(MethodBase mi)
        {
            CilReaderTestsCore.Test_CilReader_StaticFieldAccess(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "GetInterfaceCount", MethodSource.All)]
        public void Test_CilReader_VirtualCall(MethodBase mi)
        {
            CilReaderTestsCore.Test_CilReader_VirtualCall(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintList", MethodSource.All)]
        public void Test_CilReader_GenericType(MethodBase mi)
        {
            CilReaderTestsCore.Test_CilReader_GenericType(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "GenerateArray", MethodSource.All)]
        public void Test_CilReader_GenericParameter(MethodBase mi)
        {
            CilReaderTestsCore.Test_CilReader_GenericParameter(mi);
        }

#if NETFRAMEWORK
        [TestMethod]
        public void Test_CilReader_ExternalAssemblyAccess()
        {
            //Disabled on .NET Core: method GetExtension has multiple overloads 
            //now, so this lookup crashes with ambigous match error.
            MethodInfo mi = typeof(System.IO.Path).GetMethod("GetExtension");
            CilReaderTestsCore.Test_CilReader_ExternalAssemblyAccess(mi);
        }
#endif

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", MethodSource.All)]
        public void Test_CilReader_CanIterateTwice(MethodBase mi)
        {
            CilReaderTestsCore.Test_CilReader_CanIterateTwice(mi);
        }
    }
}
