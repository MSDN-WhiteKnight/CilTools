/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// * Tests that use IlAsm to produce assembly with test data method *

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class IlAsmTests
    {
        static void CheckEnvironment()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Assert.Inconclusive("IlAsm is not available on non-Windows platforms");
            }
        }

        /// <summary>
        /// Shared test logic for indirect call tests
        /// </summary>
        public static void IndirectCall_VerifyMethod(MethodBase m)
        {
            //create CilGraph from method
            CilGraph graph = CilGraph.Create(m);

            //verify CilGraph
            AssertThat.IsCorrect(graph);

            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            AssertThat.NotEmpty(
                nodes, "The result of IndirectCallTest method parsing should not be empty collection"
            );

            AssertThat.HasOnlyOneMatch(
                nodes, (x) => x.Instruction.OpCode == OpCodes.Ldarg_0,
                "The result of IndirectCallTest method parsing should contain a single 'ldarg.0' instruction"
                );

            AssertThat.HasOnlyOneMatch(
                nodes,
                (x) => x.Instruction.OpCode == OpCodes.Ldftn && (x.Instruction.ReferencedMember as MethodInfo).Name == "WriteLine",
                "The result of IndirectCallTest method parsing should contain a single 'ldftn' instruction referencing Console.WriteLine"
                );

            AssertThat.HasOnlyOneMatch(
                nodes,
                (x) => x.Instruction.OpCode == OpCodes.Calli &&
                    x.Instruction.ReferencedSignature != null &&
                    x.Instruction.ReferencedSignature.ReturnType.Type.Name == "Void" &&
                    x.Instruction.ReferencedSignature.GetParamType(0).Type.Name == "String" &&
                    x.Instruction.ReferencedSignature.CallingConvention == CallingConvention.Default,
                "The result of IndirectCallTest method parsing should contain a single 'calli' instruction with signature matching Console.WriteLine"
                );

            AssertThat.HasOnlyOneMatch(
                nodes, (x) => x.Instruction.OpCode == OpCodes.Ret,
                "The result of IndirectCallTest method parsing should contain a single 'ret' instruction"
                );

            //Verify CilGraph.ToString() output
            string str = graph.ToText();

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any, "void", Text.Any,
                "IndirectCallTest", Text.Any,
                "(",Text.Any, "string",Text.Any, ")", Text.Any,
                "cil", Text.Any, "managed", Text.Any,
                "{", Text.Any,
                "ldarg.0", Text.Any,
                "ldftn", Text.Any, "System.Console::WriteLine", Text.Any,
                "calli", Text.Any, "void", Text.Any,
                 "(",Text.Any, "string",Text.Any, ")", Text.Any,
                "ret", Text.Any,
                "}"
            });
        }

        [TestMethod]
        public void Test_IndirectCall_IlAsm()
        {
            CheckEnvironment();

            const string code = @"
.method public static void IndirectCallTest(string x) cil managed 
{ 
 .maxstack   2

          ldarg.0      
          ldftn        void [mscorlib]System.Console::WriteLine(string)
          calli        void (string)
          ret          
}";
            //compile method from CIL
            MethodBase mb = IlAsm.BuildFunction(code, "IndirectCallTest");

            //verify method executes and does not crash
            mb.Invoke(null, new object[] { "Hello from CIL Tools tests!" });

            //main test logic
            IndirectCall_VerifyMethod(mb);
        }
    }
}
