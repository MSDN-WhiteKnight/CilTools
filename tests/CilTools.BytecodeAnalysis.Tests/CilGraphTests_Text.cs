/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Reflection;
using System.Text;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class CilGraphTests_Text
    {
        const string ConditionMsg = "Codegen is different in release build";

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_CilGraph_ToString(MethodBase mi)
        {
            //Test that CilGraph.ToString returns signature
            const string expected = ".method public hidebysig static void PrintHelloWorld() cil managed";

            CilGraph graph = CilGraph.Create(mi);
            string str = graph.ToString();
            AssertThat.AreLexicallyEqual(expected, str);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TestEmptyString", BytecodeProviders.All)]
        public void Test_CilGraph_EmptyString(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);

            //Test correct empty string output
            string str = graph.ToText();

            AssertThat.IsMatch(str, new Text[] {
                Text.Any, "ldstr", Text.Any, "\"\"", Text.Any
            });
        }

        [ConditionalTest(TestCondition.DebugBuildOnly, ConditionMsg)]
        [MethodTestData(typeof(SampleMethods), "TestEmptyString", BytecodeProviders.All)]
        public void Test_CilGraph_EmptyStringIL(MethodBase mb)
        {
            const string expected = @".method public hidebysig static bool TestEmptyString(
    string str
) cil managed {
 .maxstack 2
 .locals init (bool V_0)

          nop 
          ldarg.0 
          ldstr        """"
          call         bool [mscorlib]System.String::op_Equality(string, string)
          stloc.0 
          br.s         IL_0001
 IL_0001: ldloc.0 
          ret 
}";

            //Test correct empty string disassembler output
            CilGraph graph = CilGraph.Create(mb);
            string str = graph.ToText();
            AssertThat.CilEquals(expected, str);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TestOptionalParams", BytecodeProviders.All)]
        public void Test_CilGraph_OptionalParams(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            string str = graph.ToText();

            AssertThat.IsMatch(str, new Text[] {
                Text.Any, ".method",
                Text.Any, "[opt]",Text.Any, "string",
                Text.Any, "[opt]",Text.Any, "int32",
                Text.Any, ".param", Text.Any, "[1]",
                Text.Any, "\"\"",
                Text.Any, ".param", Text.Any, "[2]",
                Text.Any, "int32(0)",
                Text.Any
            });
        }

        [ConditionalTest(TestCondition.DebugBuildOnly, ConditionMsg)]
        [MethodTestData(typeof(SampleMethods), "TestOptionalParams", BytecodeProviders.All)]
        public void Test_CilGraph_OptionalParamsIL(MethodBase mb)
        {
            const string expected = @".method public hidebysig static void TestOptionalParams(
    [opt] string str, 
    [opt] int32 x
) cil managed {
 .param [1] = """"
 .param [2] = int32(0)
 .maxstack 8

          nop 
          ldarg.0 
          ldarga.s     x
          call         instance string [mscorlib]System.Int32::ToString()
          call         string [mscorlib]System.String::Concat(string, string)
          call         void [mscorlib]System.Console::WriteLine(string)
          nop 
          ret 
}";

            //Test correct optional params disassembler output
            CilGraph graph = CilGraph.Create(mb);
            string str = graph.ToText();
            AssertThat.CilEquals(expected, str);
        }

        [TestMethod]
        [MethodTestData(typeof(Func<>), "Invoke", BytecodeProviders.All)]
        public void Test_CilGraph_ImplRuntime(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            string str = graph.ToString();

            //.method public hidebysig newslot virtual instance !0 Invoke() runtime managed

            AssertThat.IsMatch(str, new Text[] {
                Text.Any, ".method",Text.Any,
                "!",Text.Any,
                "Invoke",Text.Any,
                "(", Text.Any,")",Text.Any
                ,"runtime",Text.Any
                ,"managed",Text.Any
            });
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "AttributeTest", BytecodeProviders.Reflection)]
        public void Test_CilGraph_Attributes(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            string str = graph.ToText();

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any,
                ".custom", Text.Any,
                "instance", Text.Any,
                "void", Text.Any,
                "System.STAThreadAttribute", Text.Any,
                ".ctor", Text.Any,
                "(", Text.Any,
                "01 00 00 00", Text.Any,
                ")", Text.Any
                });

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any,
                ".custom", Text.Any,
                "instance", Text.Any,
                "void", Text.Any,
                "CilTools.Tests.Common.MyAttribute", Text.Any,
                ".ctor", Text.Any,
                "(", Text.Any,
                "int32", Text.Any,
                ")", Text.Any,
                Text.Any,
                });
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TestEscaping", BytecodeProviders.All)]
        public void Test_CilGraph_Escaping(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            string str = graph.ToText();

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any,"TestEscaping", Text.Any,
                "ldstr", Text.Any,
                "\"\\042English - Русский - Ελληνικά - Español\\015\\n\\tąęėšų,.\\042\"", Text.Any,
                });
        }

        [ConditionalTest(TestCondition.DebugBuildOnly, ConditionMsg)]
        [MethodTestData(typeof(SampleMethods), "CreatePoint", BytecodeProviders.All)]
        public void Test_CilGraph_Locals(MethodBase mi)
        {
            const string expected = @".maxstack 2
.locals init (class [CilTools.Tests.Common]CilTools.Tests.Common.MyPoint V_0,
    class [CilTools.Tests.Common]CilTools.Tests.Common.MyPoint V_1)";

            CilGraph graph = CilGraph.Create(mi);

            StringBuilder sb = new StringBuilder(100);
            StringWriter wr = new StringWriter(sb);
            graph.PrintHeader(wr);
            wr.Flush();
            string str = sb.ToString();

            AssertThat.AreLexicallyEqual(expected, str);
        }
    }
}
