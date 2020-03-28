using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CilBytecodeParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilBytecodeParser.Tests
{
    [TestClass]
    public class CilInstructionTests
    {
        [TestMethod]
        public void Test_CilInstruction_Roundtrip()
        {
            CilInstruction instr = new CilInstruction(OpCodes.Nop);
            string str = instr.ToString();
            Assert.IsTrue(str.Contains("nop"), "The result of instr.ToString() should contain instruction name");
            CilInstruction instr2 = CilInstruction.Parse(str);
            Assert.AreEqual<OpCode>(OpCodes.Nop,instr2.OpCode, "The result of CilInstruction.Parse doesn't have expected opcode");
            Assert.IsNull(instr2.Operand, "The 'nop' instruction should not have operand");

            instr = new CilInstruction(OpCodes.Ldc_I4, 1, sizeof(int));
            str = instr.ToString();
            Assert.IsTrue(str.Contains("ldc.i4"), "The result of instr.ToString() should contain instruction name");
            AssertThat.IsMatch(str, new MatchElement[] { new Literal("ldc.i4"), MatchElement.Any,new Literal("1") });
            instr2 = CilInstruction.Parse(str);
            Assert.AreEqual<OpCode>(OpCodes.Ldc_I4, instr2.OpCode, "The result of CilInstruction.Parse doesn't have expected opcode");
            Assert.AreEqual<int>(1, (int)instr2.Operand, "The result of CilInstruction.Parse doesn't have expected operand");
        }
    }
}
