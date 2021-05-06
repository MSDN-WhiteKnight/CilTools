/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests.NetCore
{
    [TestClass]
    public class CilReaderTests
    {
        [TestMethod]
        public void Test_CilReader_HelloWorld()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            CilReaderTestsCore.Test_CilReader_HelloWorld(mi);
        }

        [TestMethod]
        public void Test_CilReader_CalcSum()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("CalcSum");
            CilReaderTestsCore.Test_CilReader_CalcSum(mi);
        }

        [TestMethod]
        public void Test_CilReader_StaticFieldAccess()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("SquareFoo");
            CilReaderTestsCore.Test_CilReader_StaticFieldAccess(mi);
        }

        [TestMethod]
        public void Test_CilReader_VirtualCall()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("GetInterfaceCount");
            CilReaderTestsCore.Test_CilReader_VirtualCall(mi);
        }

        [TestMethod]
        public void Test_CilReader_GenericType()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintList");
            CilReaderTestsCore.Test_CilReader_GenericType(mi);
        }

        [TestMethod]
        public void Test_CilReader_GenericParameter()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("GenerateArray");
            CilReaderTestsCore.Test_CilReader_GenericParameter(mi);
        }

        [TestMethod]
        public void Test_CilReader_ExternalAssemblyAccess()
        {
            MethodInfo mi = typeof(System.IO.Path).GetMethod("GetExtension",new Type[] { typeof(string) });
            CilReaderTestsCore.Test_CilReader_ExternalAssemblyAccess(mi);
        }

        [TestMethod]
        public void Test_CilReader_CanIterateTwice()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            CilReaderTestsCore.Test_CilReader_CanIterateTwice(mi);
        }
    }
}
