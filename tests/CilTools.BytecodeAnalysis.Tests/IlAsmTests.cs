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
using CilTools.Reflection;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// * Tests that use IlAsm to produce test data or verify disassembler output *

namespace CilTools.BytecodeAnalysis.Tests
{
    public enum TestCondition
    {
        Never = 0, Always = 1, WindowsOnly = 2
    }

    public class ConditionalTestAttribute : TestMethodAttribute
    {
        TestCondition cond;
        string mes;

        public ConditionalTestAttribute(TestCondition condition, string message)
        {
            this.cond = condition;
            this.mes = message;
        }

        static bool ShouldRun(TestCondition cond)
        {
            switch (cond)
            {
                case TestCondition.Never: return false;
                case TestCondition.Always: return true;
                case TestCondition.WindowsOnly:
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT) return true;
                    else return false;
                default: Assert.Fail("Unknown test condition");
                    return false;
            }
        }
        
        public override TestResult[] Execute(ITestMethod testMethod)
        {
            if (ShouldRun(this.cond))
            {
                TestResult[] rets = base.Execute(testMethod);
                return rets;
            }

            TestResult res = new TestResult();
            res.Outcome = UnitTestOutcome.Inconclusive;
            res.TestFailureException = new AssertInconclusiveException(this.mes);
            return new TestResult[] { res };
        }
    }
    
    [TestClass]
    public class IlAsmTests
    {
        const string ConditionMessage = "IlAsm is not available on non-Windows platforms";
        
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

        [ConditionalTest(TestCondition.WindowsOnly, ConditionMessage)]
        public void Test_IndirectCall_IlAsm()
        {
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

        [ConditionalTest(TestCondition.WindowsOnly, ConditionMessage)]
        public void Test_FaultExceptionBlock()
        {
            const string code = @"
.method  public hidebysig static int32 FaultTest(
    int32 x, int32 y
) cil managed
{
 .maxstack  2
 .locals  init (int32 V_0)

 .try
 {
           ldarg.0      
           ldarg.1      
           div          
           stloc.0      
           leave.s      IL_0001    //return x / y;
 }
 fault
 {
           ldstr        ""Exception occured""
           call         void [mscorlib]System.Console::WriteLine(string)
           endfault
 }
 IL_0001: ldloc.0      
          ret
}";
            //compile method from CIL
            MethodBase mb = IlAsm.BuildFunction(code, "FaultTest");

            //verify method executes and does not crash
            object res=mb.Invoke(null, new object[] {4, 2});            
            Assert.AreEqual(2, (int)res);

            //*** Main test logic ***
            CilGraph graph = CilGraph.Create(mb);
            AssertThat.IsCorrect(graph);
            CilGraphNode[] nodes = graph.GetNodes().ToArray();
            AssertThat.NotEmpty(nodes);

            //find node inside try block
            CilGraphNode node = nodes.Where((x) => x.Instruction.OpCode == OpCodes.Div).Single();

            //find fault block
            ExceptionBlock[] blocks = node.GetExceptionBlocks();
            Assert.AreEqual(1, blocks.Length);
            ExceptionBlock block = blocks[0];
            Assert.AreEqual(ExceptionHandlingClauseOptions.Fault, block.Flags);

            //verify contents of fault block
            AssertThat.HasOnlyOneMatch(nodes, (x) =>
            {
                return x.Instruction.OpCode == OpCodes.Ldstr &&
                x.Instruction.ReferencedString == "Exception occured" &&
                x.Instruction.ByteOffset >= block.HandlerOffset &&
                x.Instruction.ByteOffset <= block.HandlerOffset+block.HandlerLength;
            });

            AssertThat.HasOnlyOneMatch(nodes, (x) =>
            {
                return x.Instruction.OpCode == OpCodes.Call &&
                x.Instruction.ReferencedMember.Name == "WriteLine" &&
                x.Instruction.ByteOffset >= block.HandlerOffset &&
                x.Instruction.ByteOffset <= block.HandlerOffset + block.HandlerLength;
            });

            //verify disassembler output
            string str = graph.ToText();

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any, "static", Text.Any, "int32", Text.Any,
                "FaultTest", Text.Any, "(",Text.Any, ")", Text.Any,
                "cil", Text.Any, "managed", Text.Any,
                "{", Text.Any,

                ".try", Text.Any, "{", Text.Any,
                "div", Text.Any, 
                "}", Text.Any,

                "fault", Text.Any, "{", Text.Any,
                "ldstr", Text.Any, "\"Exception occured\"", Text.Any,
                "call", Text.Any, "System.Console::WriteLine", Text.Any,
                "}", Text.Any,

                "ret", Text.Any,
                "}"
            });
        }

        [ConditionalTest(TestCondition.WindowsOnly, ConditionMessage)]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.Metadata)]
        [MethodTestData(typeof(SampleMethods), "PrintTenNumbers", BytecodeProviders.Metadata)]
        [MethodTestData(typeof(SampleMethods), "PrintList", BytecodeProviders.Metadata)]
        [MethodTestData(typeof(SampleMethods), "AttributeTest", BytecodeProviders.Metadata)]
        [MethodTestData(typeof(SampleMethods), "GenericsTest", BytecodeProviders.Metadata)]
        [MethodTestData(typeof(SampleMethods), "TestEscaping", BytecodeProviders.Metadata)]
        public void Test_Disassembler_Roundtrip(MethodBase m)
        {
            CilGraph graph = CilGraph.Create(m);
            string code = graph.ToText();

            //compile method from CIL
            MethodBase mAssembled = IlAsm.BuildFunction(code, m.Name);
            Assert.IsNotNull(mAssembled);

            //verify method executes and does not crash
            mAssembled.Invoke(null, new object[0]);
        }
    }
}
