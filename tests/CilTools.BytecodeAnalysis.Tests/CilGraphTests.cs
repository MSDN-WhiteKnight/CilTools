﻿/* CilTools.BytecodeAnalysis library tests
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

            AssertThat.IsMatch(str, new MatchElement[] {
                ".method", MatchElement.Any, "static", MatchElement.Any,
                "string", MatchElement.Any,
                "FilteredExceptionsTest", MatchElement.Any,
                "(",MatchElement.Any, ")", MatchElement.Any,
                "cil", MatchElement.Any, "managed", MatchElement.Any,
                "{", MatchElement.Any,
                
                ".try", MatchElement.Any, "{", MatchElement.Any,                
                "call", MatchElement.Any, "System.IO.File::ReadAllLines", MatchElement.Any,
                "}", MatchElement.Any,

                "filter", MatchElement.Any, "{", MatchElement.Any,
                "callvirt", MatchElement.Any, "get_Message", MatchElement.Any,
                "callvirt", MatchElement.Any, "Contains", MatchElement.Any,
                "endfilter", MatchElement.Any,"}", MatchElement.Any,

                //handler
                "{", MatchElement.Any,
                "ldsfld", MatchElement.Any, "System.String::Empty", MatchElement.Any,
                "}", MatchElement.Any,

                "ret", MatchElement.Any,
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
