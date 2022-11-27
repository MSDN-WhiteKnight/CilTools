/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using CilTools.Metadata;
using CilTools.Reflection;
using CilTools.Tests.Common;
using CilTools.Tests.Common.Attributes;
using CilTools.Tests.Common.TestData;
using CilTools.Tests.Common.TextUtils;
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
                "\"\\042English - Русский - Ελληνικά - Español\\015\\n\\tąęėšų,.\\\\\\042\"", Text.Any,
                });
        }

        [TestMethod]
        [WorkItem(68)]
        public void Test_CilGraph_Escaping_Identifier()
        {
            //emit test data assembly
            string methodName = "<TestMethod>";
            AssemblyEmitter emitter = new AssemblyEmitter("MyAssembly", AssemblyEmitter.EmitMethodBody_Empty, methodName);
            byte[] bytes = emitter.GetAssemblyBytes();
            MemoryImage img = new MemoryImage(bytes, "MyAssembly.dll", true);
            AssemblyReader reader = new AssemblyReader();
            string str;
            
            using (reader)
            {
                //disassemble method
                Assembly ass = reader.LoadImage(img);
                Type t = ass.GetType("MyAssembly.Program");
                MethodInfo mi = t.GetMethod(methodName);
                CilGraph graph = CilGraph.Create(mi);
                str = graph.ToString();
            }

            //verify output
            const string expected = ".method public hidebysig static void '<TestMethod>'() cil managed";
            AssertThat.AreLexicallyEqual(expected, str);
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

        static string GetDefaultsString(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            StringBuilder sb = new StringBuilder(100);
            StringWriter wr = new StringWriter(sb);
            graph.PrintDefaults(wr);
            wr.Flush();
            return sb.ToString();
        }

        static string GetHeaderString(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            StringBuilder sb = new StringBuilder(100);
            StringWriter wr = new StringWriter(sb);
            graph.PrintHeader(wr);
            wr.Flush();
            return sb.ToString();
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "ReturnTypeAttributeTest", BytecodeProviders.All)]
        public void Test_CilGraph_ReturnTypeCustomAttributes(MethodBase mi)
        {
            string il = GetDefaultsString(mi);

            AssertThat.IsMatch(il, new Text[] { Text.Any, ".param", Text.Any, "[0]", Text.Any, ".custom", Text.Any,
                "instance void CilTools.Tests.Common.MyAttribute::.ctor(int32)", Text.Any
            });
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "ReturnTypeAttributeTest", BytecodeProviders.Metadata)]
        public void Test_CilGraph_ReturnTypeCustomAttributes2(MethodBase mi)
        {
            const string expected = @".param [0] 
.custom instance void CilTools.Tests.Common.MyAttribute::.ctor(int32) = ( 01 00 E7 03 00 00 00 00 )";

            string il = GetDefaultsString(mi);
            AssertThat.CilEquals(expected, il);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_CilGraph_ReturnTypeCustomAttributes_Negative(MethodBase mi)
        {
            string il = GetDefaultsString(mi);
            AssertThat.CilEquals(string.Empty, il);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "ParameterAttributesTest", BytecodeProviders.All)]
        public void Test_CilGraph_ParameterCustomAttributes(MethodBase mi)
        {
            string il = GetDefaultsString(mi);

            AssertThat.IsMatch(il, new Text[] { Text.Any, ".param", Text.Any, "[1]", Text.Any, ".custom", Text.Any,
                "instance void CilTools.Tests.Common.MyAttribute::.ctor(int32)", Text.Any
            });
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "ParameterAttributesTest", BytecodeProviders.Metadata)]
        public void Test_CilGraph_ParameterCustomAttributes2(MethodBase mi)
        {
            const string expected = @".param [1]
.custom instance void CilTools.Tests.Common.MyAttribute::.ctor(int32) = ( 01 00 7B 00 00 00 00 00 )";

            string il = GetDefaultsString(mi);
            AssertThat.CilEquals(expected, il);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "CalcSum", BytecodeProviders.All)]
        public void Test_CilGraph_ParameterCustomAttributes_Negative(MethodBase mi)
        {
            string il = GetDefaultsString(mi);
            AssertThat.CilEquals(string.Empty, il);
        }

        [TestMethod]
        [MethodTestData(typeof(InterfacesSampleType), "CilTools.Tests.Common.TestData.ITest.Foo", BytecodeProviders.All)]
        public void Test_CilGraph_Override(MethodBase mi)
        {
            const string expected = @".override [CilTools.Tests.Common]CilTools.Tests.Common.TestData.ITest::Foo
 .maxstack 8";

            string il = GetHeaderString(mi);
            AssertThat.CilEquals(expected, il);
        }

        [TestMethod]
        [MethodTestData(typeof(InterfacesSampleType), "Bar", BytecodeProviders.All)]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_CilGraph_Override_Negative(MethodBase mi)
        {
            string il = GetHeaderString(mi);
            Assert.IsFalse(il.Contains(".override"));
        }

        [TestMethod]
        [MethodTestData(typeof(List<>), "System.Collections.Generic.ICollection<T>.get_IsReadOnly", BytecodeProviders.All)]
        public void Test_CilGraph_Override_GenericInterface(MethodBase mi)
        {
            string il = GetHeaderString(mi);
            AssertThat.IsMatch(il, new Text[] {
                ".override", Text.Any, "method instance bool class", Text.Any, 
                "System.Collections.Generic.ICollection`1<!", Text.Any, ">::get_IsReadOnly"
            });
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TestByRefTypes", BytecodeProviders.All)]
        public void Test_ToString_ByRefTypes(MethodBase m)
        {
            const string expected = ".method public hidebysig static void TestByRefTypes( " +
                "valuetype [mscorlib]System.DateTime& x, class [mscorlib]System.Attribute& y ) cil managed";

            CilGraph graph = CilGraph.Create(m);
            string str = graph.ToString();
            AssertThat.CilEquals(expected, str);
        }

        [TestMethod]
        [TypeTestData(typeof(SampleMethods), BytecodeProviders.All)]
        public void Test_CilGraph_StaticConstructor(Type t)
        {
            ConstructorInfo ci = t.GetConstructor(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, 
                null, new Type[0], null);

            CilGraph graph = CilGraph.Create(ci);
            string str = graph.ToText();
            string expected = ".method private hidebysig specialname rtspecialname static void .cctor() cil managed";
            AssertThat.CilContains(str, expected);
        }

        [TestMethod]
        [TypeTestData(typeof(MyPoint), BytecodeProviders.All)]
        public void Test_CilGraph_InstanceConstructor(Type t)
        {
            ConstructorInfo ci = t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new Type[0], null);

            CilGraph graph = CilGraph.Create(ci);
            string str = graph.ToText();
            string expected = ".method public hidebysig specialname rtspecialname instance void .ctor() cil managed";
            AssertThat.CilContains(str, expected);
        }

        public static void TestTokens_GenericType()
        {
            Console.WriteLine(typeof(List<>));
            Console.WriteLine(typeof(List<int>));
            Console.WriteLine(typeof(ArraySegment<>));
            Console.WriteLine(typeof(ArraySegment<string>));
        }

        [ConditionalTest(TestCondition.DebugBuildOnly, "Codegen is different in release build")]
        [MethodTestData(typeof(CilGraphTests_Text), "TestTokens_GenericType", BytecodeProviders.Metadata)]
        public void Test_CilGraph_Tokens_GenericType(MethodBase mi)
        {
            const string expected = @".method public hidebysig static void TestTokens_GenericType() cil managed
{
 .maxstack  1

          nop          
          ldtoken      [mscorlib]System.Collections.Generic.List`1
          call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
          call         void [mscorlib]System.Console::WriteLine(object)
          nop          
          ldtoken      class [mscorlib]System.Collections.Generic.List`1<int32>
          call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
          call         void [mscorlib]System.Console::WriteLine(object)
          nop          
          ldtoken      [mscorlib]System.ArraySegment`1
          call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
          call         void [mscorlib]System.Console::WriteLine(object)
          nop          
          ldtoken      valuetype [mscorlib]System.ArraySegment`1<string>
          call         class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
          call         void [mscorlib]System.Console::WriteLine(object)
          nop          
          ret          
}";

            CilGraph graph = CilGraph.Create(mi);

            //Test conversion to string
            string str = graph.ToText();
            AssertThat.CilEquals(expected, str);
        }

        [TestMethod]
        [MethodTestData(typeof(CilGraphTests_Text), "TestTokens_GenericType", BytecodeProviders.All)]
        public void Test_CilGraph_Tokens_GenericType2(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);

            //Test conversion to string
            string str = graph.ToText();

            AssertThat.IsMatch(str, new Text[] { 
                "ldtoken", Text.Any, "System.Collections.Generic.List`1", Text.Any,
                "ldtoken", Text.Any, "System.Collections.Generic.List`1", Text.Any,
                "ldtoken", Text.Any, "System.ArraySegment`1", Text.Any,
                "ldtoken", Text.Any, "System.ArraySegment`1", Text.Any,
            });
        }

        static InstructionEncoder EmitMethodBody_TestBlockClose(
            MetadataBuilder metadata,
            AssemblyReferenceHandle corlibAssemblyRef)
        {
            var codeBuilder = new BlobBuilder();
            var cfb = new ControlFlowBuilder();
            var encoder = new InstructionEncoder(codeBuilder, cfb);

            AssemblyReferenceHandle consoleAssemblyRef = AssemblyEmitter.AddAssemblyReference(
                metadata,
                "System.Console",
                new Version(4, 0, 0, 0));

            TypeReferenceHandle systemConsoleTypeRefHandle = metadata.AddTypeReference(
                consoleAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Console"));

            TypeReferenceHandle environmentTypeRefHandle = metadata.AddTypeReference(
                corlibAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Environment"));

            var consoleWriteLineSignature = new BlobBuilder();

            new BlobEncoder(consoleWriteLineSignature).
                MethodSignature().
                Parameters(1,
                    returnType => returnType.Void(),
                    parameters => parameters.AddParameter().Type().String());

            MemberReferenceHandle consoleWriteLineMemberRef = metadata.AddMemberReference(
                systemConsoleTypeRefHandle,
                metadata.GetOrAddString("WriteLine"),
                metadata.GetOrAddBlob(consoleWriteLineSignature));

            var tickCountSignature = new BlobBuilder();

            new BlobEncoder(tickCountSignature).
                MethodSignature().
                Parameters(0, returnType => returnType.Type().Int32(), parameters => { });

            MemberReferenceHandle tickCountMemberRef = metadata.AddMemberReference(
                environmentTypeRefHandle,
                metadata.GetOrAddString("get_TickCount"),
                metadata.GetOrAddBlob(tickCountSignature));

            LabelHandle label1 = encoder.DefineLabel();
            LabelHandle label2 = encoder.DefineLabel();
            LabelHandle labelTryStart = encoder.DefineLabel();
            LabelHandle labelTryEnd = encoder.DefineLabel();
            LabelHandle labelFinallyStart = encoder.DefineLabel();
            LabelHandle labelFinallyEnd = encoder.DefineLabel();

            // IL_0001: call int32 System.Environment::get_TickCount()
            encoder.MarkLabel(label1);
            encoder.Call(tickCountMemberRef);

            //ldc.i4 999
            encoder.LoadConstantI4(999);

            //ble.s IL_0002
            encoder.Branch(ILOpCode.Ble_s, label2);

            // ret
            encoder.OpCode(ILOpCode.Ret);

            //IL_0002: nop
            encoder.MarkLabel(label2);
            encoder.OpCode(ILOpCode.Nop);

            //.try
            encoder.MarkLabel(labelTryStart);

            //call int32 System.Environment::get_TickCount()
            encoder.Call(tickCountMemberRef);

            // call void System.Console::WriteLine(int32)
            encoder.Call(consoleWriteLineMemberRef);

            //leave.s IL_0001
            encoder.Branch(ILOpCode.Leave_s, label1);
            encoder.MarkLabel(labelTryEnd);

            //finally
            encoder.MarkLabel(labelFinallyStart);

            //call int32 System.Environment::get_TickCount()
            encoder.Call(tickCountMemberRef);

            // call void System.Console::WriteLine(int32)
            encoder.Call(consoleWriteLineMemberRef);

            // endfinnally
            encoder.OpCode(ILOpCode.Endfinally);
            encoder.MarkLabel(labelFinallyEnd);

            cfb.AddFinallyRegion(labelTryStart, labelTryEnd, labelFinallyStart, labelFinallyEnd);
            return encoder;
        }

        [TestMethod]
        public void Test_CilGraph_BlockClosesAfterLastInstruction()
        {
            //emit test data assembly
            string methodName = "TestBlockClose";
            AssemblyEmitter emitter = new AssemblyEmitter("MyAssembly", EmitMethodBody_TestBlockClose, methodName);
            byte[] bytes = emitter.GetAssemblyBytes();
            MemoryImage img = new MemoryImage(bytes, "MyAssembly.dll", true);
            AssemblyReader reader = new AssemblyReader();
            string str;

            using (reader)
            {
                //disassemble method
                Assembly ass = reader.LoadImage(img);
                Type t = ass.GetType("MyAssembly.Program");
                MethodInfo mi = t.GetMethod(methodName);
                CilGraph graph = CilGraph.Create(mi);
                str = graph.ToText();
            }

            //verify output
            const string expected = @".method public hidebysig static void TestBlockClose() cil managed 
{
 .maxstack  8

 IL_0001: call         int32 [System.Runtime]System.Environment::get_TickCount()
          ldc.i4       999
          ble.s        IL_0002
          ret          
 IL_0002: nop          
 .try 
 {
           call         int32 [System.Runtime]System.Environment::get_TickCount()
           call         void [System.Console]System.Console::WriteLine(string)
           leave.s      IL_0001
 }
 finally
 {
           call         int32 [System.Runtime]System.Environment::get_TickCount()
           call         void [System.Console]System.Console::WriteLine(string)
           endfinally   
 }
}";
            AssertThat.AreLexicallyEqual(expected, str);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TestNestedTypes", BytecodeProviders.All)]
        public void Test_ToString_NestedType(MethodBase mi)
        {
            const string expected = ".method public hidebysig static void TestNestedTypes(" +
                " valuetype [mscorlib]System.Environment/SpecialFolder sf ) cil managed";

            CilGraph graph = CilGraph.Create(mi);
            string str = graph.ToString();
            AssertThat.CilEquals(expected, str);
        }
    }
}
