/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class CilAnalysisTests
    {
        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintProcessId", BytecodeProviders.All)]
        public void Test_GetReferencedMethods(MethodBase m)
        {
            CilAnalysisTestsCore.Test_GetReferencedMethods(m);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "SquareFoo", BytecodeProviders.All)]
        public void Test_GetReferencedMembers(MethodBase m)
        {
            CilAnalysisTestsCore.Test_GetReferencedMembers(m);
        }

        [TestMethod]        
        public void Test_EscapeString()
        {
            string str = "\"English - Русский - Ελληνικά - Español\r\n\t\0\a\b\x0001ąęėšų,.\"";
            string result = CilAnalysis.EscapeString(str);
            string expected = "\\042English - Русский - Ελληνικά - Español\\015\\n\\t\\000\\007\\010\\001ąęėšų,.\\042";
            Assert.IsTrue(expected.Equals(result,StringComparison.Ordinal));
        }
    }
}
