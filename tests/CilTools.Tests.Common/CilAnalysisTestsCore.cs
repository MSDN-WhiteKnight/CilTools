/* CIL Tools tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.BytecodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Tests.Common
{
    public class CilAnalysisTestsCore
    {
        public static void Test_GetReferencedMethods(MethodBase mb)
        {
            MethodBase[] methods = CilAnalysis.GetReferencedMethods(mb).ToArray();

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

        public static void Test_GetReferencedMembers(MethodBase mb)
        {
            MemberInfo[] methods = CilAnalysis.GetReferencedMembers(mb).ToArray();

            AssertThat.HasOnlyOneMatch(
                methods,
                (x) => x.MemberType == MemberTypes.Field && x.Name == "Foo" && x.DeclaringType.Name == "SampleMethods",
                "SquareFoo should reference only SampleMethods.Foo");                        
        }
    }
}
