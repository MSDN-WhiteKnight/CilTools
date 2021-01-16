/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilTools.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Tests.Common
{
    public class CilGraphNodeTestsCore
    {
        public static void Test_CilGraphNode_Exceptions(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].Instruction.OpCode == OpCodes.Div)
                {
                    ExceptionBlock[] blocks = nodes[i].GetExceptionBlocks();

                    AssertThat.HasOnlyOneMatch(blocks, (x) =>
                    {
                        return x.Flags == ExceptionHandlingClauseOptions.Clause &&
                        String.Equals(
                            x.CatchType.Name, "DivideByZeroException", StringComparison.InvariantCulture
                            );
                    });

                    AssertThat.HasOnlyOneMatch(blocks, (x) =>
                    {
                        return x.Flags.HasFlag(ExceptionHandlingClauseOptions.Finally);
                    });

                    return;
                }
            }

            Assert.Fail("div instruction not found");
        }
    }
}
