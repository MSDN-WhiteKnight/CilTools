/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
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
        public void Test_CilGraph_ToString()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            CilGraphTestsCore_Text.Test_CilGraph_ToString(mi);
        }

        [TestMethod]
        public void Test_CilGraph_EmptyString()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("TestEmptyString");
            CilGraphTestsCore_Text.Test_CilGraph_EmptyString(mi);
        }

        [TestMethod]
        public void Test_CilGraph_OptionalParams()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("TestOptionalParams");
            CilGraphTestsCore_Text.Test_CilGraph_OptionalParams(mi);
        }

        [TestMethod]
        public void Test_CilGraph_ImplRuntime()
        {
            MethodInfo mi = typeof(System.Func<>).GetMethod("Invoke");
            CilGraphTestsCore_Text.Test_CilGraph_ImplRuntime(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Attributes()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("AttributeTest");
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
        public void Test_CilGraph_Locals()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("CreatePoint");
            CilGraphTestsCore_Text.Test_CilGraph_Locals(mi);
        }
#endif
    }
}
