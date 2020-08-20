/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class CilAnalysisTests
    {
        [TestMethod]
        public void Test_GetReferencedMethods()
        {
            MethodBase[] methods = CilAnalysis.GetReferencedMethods(typeof(SampleMethods).GetMethod("PrintProcessId")).ToArray();

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
        public void Test_GetReferencedMembers()
        {
            MemberInfo[] methods = CilAnalysis.GetReferencedMembers(typeof(SampleMethods).GetMethod("SquareFoo")).ToArray();

            AssertThat.HasOnlyOneMatch(
                methods,
                (x) => x.MemberType == MemberTypes.Field && x.Name == "Foo" && x.DeclaringType.Name == "SampleMethods",
                "SquareFoo should reference only SampleMethods.Foo");                        
        }
    }
}
