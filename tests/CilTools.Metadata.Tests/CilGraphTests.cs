﻿/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
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
        public void Test_CilGraph_Loop()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase mi = t.GetMember("PrintTenNumbers")[0] as MethodBase;
                CilGraphTestsCore.Test_CilGraph_Loop(mi);
            }
        }

        [TestMethod]
        public void Test_CilGraph_Exceptions()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase mi = t.GetMember("DivideNumbers")[0] as MethodBase;
                CilGraphTestsCore.Test_CilGraph_Exceptions(mi);
            }
        }

        [TestMethod]
        public void Test_CilGraph_GetHandlerNodes()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase mi = t.GetMember("DivideNumbers")[0] as MethodBase;
                CilGraphTestsCore.Test_CilGraph_GetHandlerNodes(mi);
            }
        }

        [TestMethod]
        public void Test_CilGraph_Tokens()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase mi = t.GetMember("TestTokens")[0] as MethodBase;
                CilGraphTestsCore.Test_CilGraph_Tokens(mi);
            }
        }

        [TestMethod]
        public void Test_CilGraph_Constrained()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase mi = t.GetMember("ConstrainedTest")[0] as MethodBase;
                CilGraphTestsCore.Test_CilGraph_Constrained(mi);
            }
        }

#if DEBUG
        [TestMethod]
        public void Test_CilGraph_Pointer()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase mi = t.GetMember("PointerTest")[0] as MethodBase;
                CilGraphTestsCore.Test_CilGraph_Pointer(mi);
            }
        }
#endif
    }
}
