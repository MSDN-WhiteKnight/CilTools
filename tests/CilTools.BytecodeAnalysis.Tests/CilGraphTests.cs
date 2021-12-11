/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Linq;
using System.Reflection;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using System.Reflection.Emit;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class CilGraphTests
    {
        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_CilGraph_HelloWorld(MethodBase mi)
        {
            CilGraphTestsCore.Test_CilGraph_HelloWorld(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintTenNumbers", BytecodeProviders.All)]
        public void Test_CilGraph_Loop(MethodBase mi)
        {
            CilGraphTestsCore.Test_CilGraph_Loop(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "DivideNumbers", BytecodeProviders.All)]
        public void Test_CilGraph_Exceptions(MethodBase mi)
        {
            CilGraphTestsCore.Test_CilGraph_Exceptions(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "DivideNumbers", BytecodeProviders.All)]
        public void Test_CilGraph_GetHandlerNodes(MethodBase mi)
        {
            CilGraphTestsCore.Test_CilGraph_GetHandlerNodes(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TestTokens", BytecodeProviders.All)]
        public void Test_CilGraph_Tokens(MethodBase mi)
        {
            CilGraphTestsCore.Test_CilGraph_Tokens(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "ConstrainedTest", BytecodeProviders.All)]
        public void Test_CilGraph_Constrained(MethodBase mi)
        {
            CilGraphTestsCore.Test_CilGraph_Constrained(mi);
        }

        [TestMethod]
        [WorkItem(49)]
        public void Test_CilGraph_DynamicMethod()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                //Skipped on Linux (https://github.com/MSDN-WhiteKnight/CilTools/issues/49)
                Assert.Inconclusive("Dynamic methods are not supported on non-Windows platforms");
            }

            //create dynamic method
            DynamicMethod dm = new DynamicMethod(
                "DynamicMethodTest", typeof(int), new Type[] { typeof(string) }, typeof(SampleMethods).Module
                );
            ILGenerator ilg = dm.GetILGenerator(2048);
            ilg.DeclareLocal(typeof(string));

            ilg.BeginExceptionBlock();
            ilg.Emit(OpCodes.Ldstr, "Hello, world.");

            ilg.Emit(OpCodes.Stloc, (short)0);
            ilg.Emit(OpCodes.Ldloc, (short)0);
            ilg.Emit(
                OpCodes.Call,
                typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) })
                );

            ilg.Emit(OpCodes.Ldsfld, typeof(SampleMethods).GetField("f"));
            ilg.Emit(
                OpCodes.Call,
                typeof(Console).GetMethod("WriteLine", new Type[] { typeof(int) })
                );

            ilg.BeginCatchBlock(typeof(Exception));
            ilg.Emit(
                OpCodes.Call,
                typeof(Console).GetMethod("WriteLine", new Type[] { typeof(object) })
                );

            ilg.BeginFinallyBlock();

            ilg.Emit(OpCodes.Ldc_I4, 10);
            ilg.Emit(OpCodes.Newarr, typeof(Guid));
            ilg.Emit(OpCodes.Pop);
            ilg.EndExceptionBlock();

            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ret);

            var deleg = (Func<string, int>)dm.CreateDelegate(typeof(Func<string, int>));
            int res = deleg("Hello, world!");

            Console.WriteLine(Environment.Version.ToString());
            Diagnostics.Error += (x, y) => { Console.WriteLine(y.Exception.ToString()); };
            CilGraphTestsCore.Test_CilGraph_DynamicMethod(dm);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "FilteredExceptionsTest", BytecodeProviders.All)]
        public void Test_CilGraph_ExceptionFilters(MethodBase m)
        {
            CilGraph graph = CilGraph.Create(m);
            AssertThat.IsCorrect(graph);
            
            CilGraphNode[] nodes = graph.GetNodes().ToArray();
            AssertThat.NotEmpty(nodes);

            //find node inside try block
            CilGraphNode node = nodes.
                Where((x) => x.Instruction.OpCode == OpCodes.Call && x.Instruction.ReferencedMember.Name == "ReadAllLines").
                Single();

            //find filter block
            ExceptionBlock[] blocks=node.GetExceptionBlocks();
            Assert.AreEqual(1, blocks.Length);
            ExceptionBlock block=blocks[0];
            Assert.AreEqual(ExceptionHandlingClauseOptions.Filter, block.Flags);

            //verify contents of filter block
            AssertThat.HasOnlyOneMatch(nodes, (x) =>
            {
                return x.Instruction.OpCode == OpCodes.Callvirt && 
                x.Instruction.ReferencedMember.Name == "get_Message" &&
                x.Instruction.ByteOffset >= block.FilterOffset &&
                x.Instruction.ByteOffset <= block.HandlerOffset;
            });

            AssertThat.HasOnlyOneMatch(nodes, (x) =>
            {
                return x.Instruction.OpCode == OpCodes.Callvirt &&
                x.Instruction.ReferencedMember.Name == "Contains" &&
                x.Instruction.ByteOffset >= block.FilterOffset &&
                x.Instruction.ByteOffset <= block.HandlerOffset;
            });

            //verify disassembler output
            string str = graph.ToText();

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any, "static", Text.Any,
                "string", Text.Any,
                "FilteredExceptionsTest", Text.Any,
                "(",Text.Any, ")", Text.Any,
                "cil", Text.Any, "managed", Text.Any,
                "{", Text.Any,
                
                ".try", Text.Any, "{", Text.Any,                
                "call", Text.Any, "System.IO.File::ReadAllLines", Text.Any,
                "}", Text.Any,

                "filter", Text.Any, "{", Text.Any,
                "callvirt", Text.Any, "get_Message", Text.Any,
                "callvirt", Text.Any, "Contains", Text.Any,
                "endfilter", Text.Any,"}", Text.Any,

                //handler
                "{", Text.Any,
                "ldsfld", Text.Any, "System.String::Empty", Text.Any,
                "}", Text.Any,

                "ret", Text.Any,
                "}"
            });
        }

#if DEBUG
        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PointerTest", BytecodeProviders.All)]
        public void Test_CilGraph_Pointer(MethodBase mi)
        {
            CilGraphTestsCore.Test_CilGraph_Pointer(mi);
        }
#endif

    }
}
