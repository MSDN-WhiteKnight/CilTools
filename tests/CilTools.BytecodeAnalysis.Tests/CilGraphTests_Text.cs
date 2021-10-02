/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;
using System.Text;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class CilGraphTests_Text
    {
        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_CilGraph_ToString(MethodBase mi)
        {
            CilGraphTestsCore_Text.Test_CilGraph_ToString(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TestEmptyString", BytecodeProviders.All)]
        public void Test_CilGraph_EmptyString(MethodBase mi)
        {
            CilGraphTestsCore_Text.Test_CilGraph_EmptyString(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TestOptionalParams", BytecodeProviders.All)]
        public void Test_CilGraph_OptionalParams(MethodBase mi)
        {
            CilGraphTestsCore_Text.Test_CilGraph_OptionalParams(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(Func<>), "Invoke", BytecodeProviders.All)]
        public void Test_CilGraph_ImplRuntime(MethodBase mi)
        {            
            CilGraphTestsCore_Text.Test_CilGraph_ImplRuntime(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "AttributeTest", BytecodeProviders.Reflection)]
        public void Test_CilGraph_Attributes(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            string str = graph.ToText();

            AssertThat.IsMatch(str, new MatchElement[] {
                new Literal(".method"), MatchElement.Any,
                new Literal(".custom"), MatchElement.Any,
                new Literal("instance"), MatchElement.Any,
                new Literal("void"), MatchElement.Any,
                new Literal("System.STAThreadAttribute"), MatchElement.Any,
                new Literal(".ctor"), MatchElement.Any,
                new Literal("("), MatchElement.Any,
                new Literal("01 00 00 00"), MatchElement.Any,
                new Literal(")"), MatchElement.Any
                });

            AssertThat.IsMatch(str, new MatchElement[] {
                new Literal(".method"), MatchElement.Any,
                new Literal(".custom"), MatchElement.Any,
                new Literal("instance"), MatchElement.Any,
                new Literal("void"), MatchElement.Any,
                new Literal("CilTools.Tests.Common.MyAttribute"), MatchElement.Any,
                new Literal(".ctor"), MatchElement.Any,
                new Literal("("), MatchElement.Any,
                new Literal("int32"), MatchElement.Any,
                new Literal(")"), MatchElement.Any,
                MatchElement.Any,
                });
        }

#if DEBUG
        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "CreatePoint", BytecodeProviders.All)]
        public void Test_CilGraph_Locals(MethodBase mi)
        {
            CilGraphTestsCore_Text.Test_CilGraph_Locals(mi);
        }
#endif
    }
}
