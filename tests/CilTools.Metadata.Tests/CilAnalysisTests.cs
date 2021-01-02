/* CIL Tools tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using CilTools.Metadata;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class CilAnalysisTests
    {
        [TestMethod]
        public void Test_GetReferencedMethods()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase mi = t.GetMember("PrintProcessId")[0] as MethodBase;
                CilAnalysisTestsCore.Test_GetReferencedMethods(mi);
            }
        }

        [TestMethod]
        public void Test_GetReferencedMembers()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase mi = t.GetMember("SquareFoo")[0] as MethodBase;
                CilAnalysisTestsCore.Test_GetReferencedMembers(mi);
            }
        }
    }
}
