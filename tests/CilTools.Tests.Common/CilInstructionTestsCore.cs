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
    public static class CilInstructionTestsCore
    {
        public static void Test_CilInstruction_Roundtrip()
        {
            CilInstruction instr = CilInstruction.CreateEmptyInstruction(null);
            string str = instr.ToString();
            Assert.IsTrue(str.Contains("nop"), "The result of instr.ToString() should contain instruction name");
            CilInstruction instr2 = CilInstruction.Parse(str);
            Assert.AreEqual<OpCode>(OpCodes.Nop, instr2.OpCode, "The result of CilInstruction.Parse doesn't have expected opcode");
            Assert.IsNull(instr2.Operand, "The 'nop' instruction should not have operand");

            instr = CilInstruction.Create<int>(OpCodes.Ldc_I4, 1, sizeof(int));
            str = instr.ToString();
            Assert.IsTrue(str.Contains("ldc.i4"), "The result of instr.ToString() should contain instruction name");
            AssertThat.IsMatch(str, new Text[] { "ldc.i4", Text.Whitespace, "1" });
            instr2 = CilInstruction.Parse(str);
            Assert.AreEqual<OpCode>(OpCodes.Ldc_I4, instr2.OpCode, "The result of CilInstruction.Parse doesn't have expected opcode");
            Assert.AreEqual<int>(1, (int)instr2.Operand, "The result of CilInstruction.Parse doesn't have expected operand");
        }

        public static void Test_CilInstruction_ToString(MethodBase mi)
        {
            CilInstruction[] instructions = CilReader.GetInstructions(mi).ToArray();

            //verify that array contains a single instruction which .ToString() output
            //matches the specified text pattern

            AssertThat.HasOnlyOneMatch(
                instructions,
                (x) =>
                {
                    string s = x.ToString();

                    //call       void [mscorlib]System.Console::WriteLine(string)

                    return Text.IsMatch(s, new Text[] {
                        "call",Text.Whitespace,
                        "void",Text.Any,
                        "System.Console::WriteLine",Text.Any,
                        "(",Text.Any,
                        "string",Text.Any,
                        ")",Text.Any,
                    });
                }
                );
        }
    }
}
