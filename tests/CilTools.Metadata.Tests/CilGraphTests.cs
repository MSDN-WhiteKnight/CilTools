﻿/* CilTools.Metadata tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class CilGraphTests
    {
        [TestMethod]
        public void Test_CilGraph_HelloWorld()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("PrintHelloWorld")[0] as MethodBase;
            CilGraphTestsCore.Test_CilGraph_HelloWorld(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Loop()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("PrintTenNumbers")[0] as MethodBase;
            CilGraphTestsCore.Test_CilGraph_Loop(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Exceptions()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("DivideNumbers")[0] as MethodBase;
            CilGraphTestsCore.Test_CilGraph_Exceptions(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Tokens()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("TestTokens")[0] as MethodBase;
            CilGraphTestsCore.Test_CilGraph_Tokens(mi);
        }

#if DEBUG
        [TestMethod]
        public void Test_CilGraph_Pointer()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("PointerTest")[0] as MethodBase;
            CilGraphTestsCore.Test_CilGraph_Pointer(mi);
        }
#endif
    }
}
