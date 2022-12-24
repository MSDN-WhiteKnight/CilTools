/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.Syntax;
using CilTools.Tests.Common;
using CilTools.Tests.Common.TextUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class CilInstructionTests
    {
        [TestMethod]
        public void Test_CilInstruction_Roundtrip()
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

        [TestMethod]
        public void Test_Parse_EndsWithComment()
        {
            string s = "    add //adds two values";
            CilInstruction instr = CilInstruction.Parse(s);
            Assert.AreEqual("add", instr.Name);
            Assert.IsNull(instr.Operand);
        }

        [TestMethod]
        public void Test_Parse_WithLabel()
        {
            string s = "IL_0001: ldc.i4 0x1F";
            CilInstruction instr = CilInstruction.Parse(s);
            Assert.AreEqual("ldc.i4", instr.Name);
            Assert.AreEqual(0x1F, instr.Operand);
        }

        [TestMethod]
        public void Test_Parse_StartsWithComment()
        {
            string s = "/*load floating point value*/ ldc.r4 1.23";
            CilInstruction instr = CilInstruction.Parse(s);
            Assert.AreEqual(OpCodes.Ldc_R4, instr.OpCode);
            Assert.AreEqual(1.23f, instr.Operand);
        }

        [DataTestMethod]
        [DataRow(" ")]
        [DataRow("//some comment")]
        [DataRow("LABEL:")]
        public void Test_Parse_Null(string s)
        {
            CilInstruction instr = CilInstruction.Parse(s);
            Assert.IsNull(instr);
        }

        [TestMethod]
        public void Test_Parse_Invalid()
        {
            string s = "public static";
            AssertThat.Throws<NotSupportedException>(() => CilInstruction.Parse(s));
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_CilInstruction_ToString(MethodBase mi)
        {
            CilInstruction[] instructions = CilReader.GetInstructions(mi).ToArray();

            //verify that array contains a single instruction which .ToString() output
            //matches the specified text pattern

            AssertThat.HasOnlyOneMatch( instructions,
                (x) => {
                    string s = x.ToString();

                    //call void [mscorlib]System.Console::WriteLine(string)

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

        [TestMethod]
        public void Test_CilInstruction_ToSyntax()
        {
            CilInstruction instr = CilInstruction.Create(OpCodes.Ldc_I4, 1, sizeof(int));
            SyntaxNode[] syntax = instr.ToSyntax().ToArray();

            Assert.AreEqual(2, syntax.Length);

            Assert.IsTrue(syntax[0] is KeywordSyntax);
            Assert.AreEqual("ldc.i4",((KeywordSyntax)syntax[0]).Content);
            Assert.AreEqual(KeywordKind.InstructionName, ((KeywordSyntax)syntax[0]).Kind);
            Assert.AreEqual(string.Empty, syntax[0].LeadingWhitespace);
            Assert.AreEqual("     ", syntax[0].TrailingWhitespace);

            Assert.IsTrue(syntax[1] is LiteralSyntax);
            Assert.AreEqual(1, (int)((LiteralSyntax)syntax[1]).Value);
            Assert.AreEqual(string.Empty, syntax[1].LeadingWhitespace);
            Assert.AreEqual(string.Empty, syntax[1].TrailingWhitespace);
        }

        [TestMethod]
        public void Test_CilInstruction_ToSyntax_Simple()
        {
            CilInstruction instr = CilInstruction.Create(OpCodes.Nop);
            SyntaxNode[] syntax = instr.ToSyntax().ToArray();

            Assert.AreEqual(1, syntax.Length);

            Assert.IsTrue(syntax[0] is KeywordSyntax);
            Assert.AreEqual("nop", ((KeywordSyntax)syntax[0]).Content);
            Assert.AreEqual(KeywordKind.InstructionName, ((KeywordSyntax)syntax[0]).Kind);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_CilInstruction_ToSyntax_Call(MethodBase mi)
        {
            CilInstruction[] instructions = CilReader.GetInstructions(mi).ToArray();
            CilInstruction instr = instructions.Where(x => x.OpCode == OpCodes.Call).Single();
            SyntaxNode[] syntax = instr.ToSyntax().ToArray();

            Assert.AreEqual(2, syntax.Length);

            Assert.IsTrue(syntax[0] is KeywordSyntax);
            Assert.AreEqual("call", ((KeywordSyntax)syntax[0]).Content);
            Assert.AreEqual(KeywordKind.InstructionName, ((KeywordSyntax)syntax[0]).Kind);
            Assert.AreEqual(string.Empty, syntax[0].LeadingWhitespace);
            Assert.AreEqual("       ", syntax[0].TrailingWhitespace);

            Assert.IsTrue(syntax[1] is MemberRefSyntax);
            Assert.AreEqual("WriteLine", ((MemberRefSyntax)syntax[1]).Member.Name);
            Assert.IsTrue(((MemberRefSyntax)syntax[1]).Member is MethodBase);
            Assert.AreEqual(string.Empty, syntax[1].LeadingWhitespace);
            Assert.AreEqual(string.Empty, syntax[1].TrailingWhitespace);
        }
    }
}
