/* CilTools.Metadata tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class CilReaderTests
    {
        [TestMethod]
        public void Test_CilReader_HelloWorld()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("PrintHelloWorld")[0] as MethodBase;
            CilReaderTestsCore.Test_CilReader_HelloWorld(mi);
        }

        [TestMethod]
        public void Test_CilReader_CalcSum()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("CalcSum")[0] as MethodBase;
            CilReaderTestsCore.Test_CilReader_CalcSum(mi);
        }

        [TestMethod]
        public void Test_CilReader_StaticFieldAccess()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("SquareFoo")[0] as MethodBase;
            CilReaderTestsCore.Test_CilReader_StaticFieldAccess(mi);
        }

        [TestMethod]
        public void Test_CilReader_VirtualCall()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("GetInterfaceCount")[0] as MethodBase;
            CilReaderTestsCore.Test_CilReader_VirtualCall(mi);
        }

        [TestMethod]
        public void Test_CilReader_GenericType()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("PrintList")[0] as MethodBase;
            CilReaderTestsCore.Test_CilReader_GenericType(mi);
        }

        [TestMethod]
        public void Test_CilReader_GenericParameter()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("GenerateArray")[0] as MethodBase;
            CilReaderTestsCore.Test_CilReader_GenericParameter(mi);
        }

        [TestMethod]
        public void Test_CilReader_ExternalAssemblyAccess()
        {
            Assembly ass = MetadataLoader.Load(typeof(System.IO.Path).Assembly.Location);
            Type t = ass.GetType("System.IO.Path");
            MethodBase mi = t.GetMember("GetExtension")[0] as MethodBase;
            CilReaderTestsCore.Test_CilReader_ExternalAssemblyAccess(mi);
        }
    }
}
