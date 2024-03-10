/* CIL Tools
 * Copyright (c) 2024,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Tests.Common;
using CilTools.Tests.Common.TextUtils;

namespace CilTools.BytecodeAnalysis.Tests.Syntax
{
    [TestClass]
    public class SyntaxGenerationTests
    {
        // Tests that verify disassembler output in different cases

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TestArrayOfArrays", BytecodeProviders.Metadata)]
        public void Test_Operand_ArrayOfArrays(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            string str = graph.ToText();

            AssertThat.IsMatch(str, new Text[] {
                Text.Any, "newarr", Text.Any, "uint8[]", Text.Any
            });
        }
    }
}
