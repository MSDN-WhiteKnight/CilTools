/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
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
            MethodBase[] methods = CilAnalysis.GetReferencedMethods(m).ToArray();

            AssertThat.HasAtLeastOneMatch(
                methods,
                (x) => x.Name == "GetCurrentProcess" && x.DeclaringType.Name == "Process",
                "PrintProcessId should reference Process.GetCurrentProcess");

            AssertThat.HasAtLeastOneMatch(
                methods,
                (x) => x.Name == typeof(System.Diagnostics.Process).GetProperty("Id").GetMethod.Name && x.DeclaringType.Name == "Process",
                "PrintProcessId should reference Process.Id getter");

            AssertThat.HasAtLeastOneMatch(
                methods,
                (x) => x.Name == "ToString" && x.DeclaringType.Name == "Int32",
                "PrintProcessId should reference Int32.ToString");

            AssertThat.HasAtLeastOneMatch(
                methods,
                (x) => x.Name == "WriteLine" && x.DeclaringType.Name == "Console",
                "PrintProcessId should reference Console.WriteLine");
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "SquareFoo", BytecodeProviders.All)]
        public void Test_GetReferencedMembers(MethodBase m)
        {
            MemberInfo[] methods = CilAnalysis.GetReferencedMembers(m).ToArray();

            AssertThat.HasOnlyOneMatch(
                methods,
                (x) => x.MemberType == MemberTypes.Field && x.Name == "Foo" && x.DeclaringType.Name == "SampleMethods",
                "SquareFoo should reference only SampleMethods.Foo");
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
