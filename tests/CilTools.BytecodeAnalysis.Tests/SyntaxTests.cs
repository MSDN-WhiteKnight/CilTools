/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Syntax;
using CilTools.Tests.Common;
using CilTools.Tests.Common.Attributes;
using CilTools.Tests.Common.TestData;
using CilTools.Tests.Common.TextUtils;

namespace CilTools.BytecodeAnalysis.Tests
{
    public class TypeWithProperties
    {
        public string Name { get; set; }
        public int Number { get; }
        public string this[int i] { get { return i.ToString(); } }
    }

    public class StaticPropertyTest
    {
        public static StaticPropertyTest Value
        {
            get { return new StaticPropertyTest(); }
        }
    }

    public class ConstantsTest
    {
        public const bool Truth = true;
        public const int One = 1;
        public const string PlanetName = "Earth";
        public const object NullValue = null;
    }

    [TestClass]
    public class SyntaxTests
    {
        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_ToSyntaxTree(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            MethodDefSyntax mds = graph.ToSyntaxTree();
            AssertThat.IsSyntaxTreeCorrect(mds);
            Assert.AreEqual("method", mds.Signature.Name);

            AssertThat.HasOnlyOneMatch(
                mds.Signature.EnumerateChildNodes(),
                (x) => { return x is KeywordSyntax && (x as KeywordSyntax).Content == "public"; },
                "Method signature should contain 'public' keyword"
                );

            AssertThat.HasOnlyOneMatch(
                mds.Signature.EnumerateChildNodes(),
                (x) => {
                    return x is IdentifierSyntax && (x as IdentifierSyntax).Content == "PrintHelloWorld";
                },
                "Method signature should contain mathod name identifier"
                );

            AssertThat.HasOnlyOneMatch(
                mds.Body.Content,
                (x) => {
                    return x is InstructionSyntax && (x as InstructionSyntax).Operation == "ldstr";
                },
                "Method body should contain 'ldstr' instruction"
                );
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "method", BytecodeProviders.All)]
        public void Test_KeywordAsIdentifier(MethodBase mi)
        {
            string str = CilAnalysis.MethodToText(mi);

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any, "public", Text.Any,
                "void", Text.Any,
                "'method'", Text.Any,
                "cil", Text.Any, "managed", Text.Any,
            });
        }

        [TestMethod]
        [TypeTestData(typeof(TypeWithProperties), BytecodeProviders.All)]
        public void Test_Properties(Type t)
        {
            IEnumerable<SyntaxNode> nodes=SyntaxNode.GetTypeDefSyntax(t);            
            string s = Utils.SyntaxToString(nodes);
                        
            AssertThat.IsMatch(s, new Text[] {
                ".class", Text.Any,"public", Text.Any,"TypeWithProperties", Text.Any,"{", Text.Any,
                ".property", Text.Any,"instance", Text.Any,"string", Text.Any,"Name", Text.Any,"()", Text.Any,
                "{", Text.Any,
                ".get", Text.Any,"instance", Text.Any,"string", Text.Any,"get_Name", Text.Any,"()", Text.Any,
                ".set", Text.Any,"instance", Text.Any,"void", Text.Any,"set_Name", Text.Any,"(", Text.Any,
                "string", Text.Any,")", Text.Any,
                "}", Text.Any,
                "}", Text.Any
            });

            AssertThat.IsMatch(s, new Text[] {
                ".class", Text.Any,"public", Text.Any,"TypeWithProperties", Text.Any,"{", Text.Any,
                ".property", Text.Any,"instance", Text.Any,"int32", Text.Any,"Number", Text.Any,"()", Text.Any,
                "{", Text.Any,
                ".get", Text.Any,"instance", Text.Any,"int32", Text.Any,"get_Number", Text.Any,"()", Text.Any,                
                "}", Text.Any,
                "}", Text.Any
            });

            AssertThat.IsMatch(s, new Text[] {
                ".class", Text.Any,"public", Text.Any,"TypeWithProperties", Text.Any,"{", Text.Any,
                ".property", Text.Any,"instance", Text.Any,"string", Text.Any,"Item",Text.Any,
                "(", Text.Any,"int32", Text.Any,")", Text.Any,
                "{", Text.Any,
                ".get", Text.Any,"instance", Text.Any,"string", Text.Any,"get_Item",
                "(", Text.Any,"int32", Text.Any,")", Text.Any,
                "}", Text.Any,
                "}", Text.Any
            });
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_Syntax_EntryPoint_Library(MethodBase m)
        {
            string str = CilAnalysis.MethodToText(m);
            Assert.IsFalse(str.Contains(".entrypoint"));
        }

        static int PrintHelloWorld_GetCodeSize()
        {
            if (Utils.GetConfig() == "Release") return 11;
            else return 13;
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_CodeSize(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            DisassemblerParams pars = new DisassemblerParams();
            pars.IncludeCodeSize = true;
            MethodDefSyntax syntax = graph.ToSyntaxTree(pars);
            string str = syntax.ToString();

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any,"public", Text.Any,"PrintHelloWorld", Text.Any,"{", Text.Any,
                "// Code size: " + PrintHelloWorld_GetCodeSize().ToString(), Text.Any,
                "call", Text.Any, "System.Console::WriteLine(string)", Text.Any, 
                "}", Text.Any
            });
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_CodeSize_Negative(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            DisassemblerParams pars = new DisassemblerParams();
            pars.IncludeCodeSize = false;
            MethodDefSyntax syntax = graph.ToSyntaxTree(pars);
            string str = syntax.ToString();

            Assert.IsFalse(str.Contains("// Code size: "));
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "Sum", BytecodeProviders.All)]
        [WorkItem(53)]
        public void Test_Syntax_GenericMethodParameterConstraints(MethodBase m)
        {
            string str = CilAnalysis.MethodToText(m);

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any, "public", Text.Any, "!!", Text.Any, "Sum", Text.Any,
                "<", Text.Any, "valuetype", Text.Any, ".ctor", Text.Any, "(", Text.Any,
                "System.ValueType", Text.Any,")", Text.Any, "T", Text.Any, ">", Text.Any,
                "{", Text.Any, "}", Text.Any
            });
        }

        [TestMethod]
        [WorkItem(53)]
        [TypeTestData(typeof(GenericConstraintsSample<>), BytecodeProviders.All)]
        public void Test_GenericTypeParameterConstraints(Type t)
        {
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t);
            string str = Utils.SyntaxToString(nodes);

            AssertThat.IsMatch(str, new Text[] {
                ".class", Text.Any, "public", Text.Any, "GenericConstraintsSample", Text.Any, 
                "<", Text.Any, "valuetype", Text.Any, ".ctor", Text.Any, "(", Text.Any,
                "System.ValueType", Text.Any,")", Text.Any, "T", Text.Any, ">", Text.Any,
                "{", Text.Any, "}", Text.Any
            });
        }

        [TestMethod]
        [WorkItem(53)]
        [TypeTestData(typeof(Action<>), BytecodeProviders.All)]
        public void Test_GenericTypeParameterFlags(Type t)
        {
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t);
            string str = Utils.SyntaxToString(nodes);

            AssertThat.IsMatch(str, new Text[] {
                ".class", Text.Any, "public", Text.Any, "Action", Text.Any,
                "<", Text.Any, "-", Text.Any, "T", Text.Any, ">", Text.Any,
                "{", Text.Any, "}", Text.Any
            });
        }

        [ConditionalTest(TestCondition.DebugBuildOnly, "Codegen is different in release builds")]
        [TypeTestData(typeof(EventsSample), BytecodeProviders.Metadata)]
        public void Test_Events_Metadata(Type t)
        {
            string expected = @".class public auto ansi beforefieldinit CilTools.Tests.Common.TestData.EventsSample
extends [mscorlib]System.Object {

 .field private class [mscorlib]System.Action A
 .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
 .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 )

 .field private static class [mscorlib]System.EventHandler`1<class [mscorlib]System.EventArgs> B
 .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
 .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 )

 .event [mscorlib]System.Action A {
  .addon instance void CilTools.Tests.Common.TestData.EventsSample::add_A(class [mscorlib]System.Action)
  .removeon instance void CilTools.Tests.Common.TestData.EventsSample::remove_A(class [mscorlib]System.Action)
 }

 .event class [mscorlib]System.EventHandler`1<class [mscorlib]System.EventArgs> B {
  .custom instance void CilTools.Tests.Common.MyAttribute::.ctor(int32) = ( 01 00 04 00 00 00 00 00 )
  .addon void CilTools.Tests.Common.TestData.EventsSample::add_B(class [mscorlib]System.EventHandler`1<class [mscorlib]System.EventArgs>)
  .removeon void CilTools.Tests.Common.TestData.EventsSample::remove_B(class [mscorlib]System.EventHandler`1<class [mscorlib]System.EventArgs>)
 }

 .event class [mscorlib]System.Action`1<int32> C {
  .addon instance void CilTools.Tests.Common.TestData.EventsSample::add_C(class [mscorlib]System.Action`1<int32>)
  .removeon instance void CilTools.Tests.Common.TestData.EventsSample::remove_C(class [mscorlib]System.Action`1<int32>)
 }

 //... 
}";

            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t);
            string s = Utils.SyntaxToString(nodes);
            AssertThat.CilEquals(expected, s);
        }

        [ConditionalTest(TestCondition.DebugBuildOnly, "Codegen is different in release builds")]
        [TypeTestData(typeof(EventsSample), BytecodeProviders.Reflection)]
        public void Test_Events_Reflection(Type t)
        {
            //this is different, because we can't get exact custom attributes IL from runtime reflection
            string expected = @".class  public auto ansi beforefieldinit CilTools.Tests.Common.TestData.EventsSample
extends [mscorlib]System.Object {
 .field private class [mscorlib]System.Action A
 .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
 //.custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState)

 .field private static class [mscorlib]System.EventHandler`1<class [mscorlib]System.EventArgs> B
 .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
 //.custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState)

 .event [mscorlib]System.Action A {
  .addon instance void CilTools.Tests.Common.TestData.EventsSample::add_A(class [mscorlib]System.Action)
  .removeon instance void CilTools.Tests.Common.TestData.EventsSample::remove_A(class [mscorlib]System.Action)
 }

 .event class [mscorlib]System.EventHandler`1<class [mscorlib]System.EventArgs> B {
  //.custom instance void [CilTools.Tests.Common]CilTools.Tests.Common.MyAttribute::.ctor(int32)
  .addon void CilTools.Tests.Common.TestData.EventsSample::add_B(class [mscorlib]System.EventHandler`1<class [mscorlib]System.EventArgs>)
  .removeon void CilTools.Tests.Common.TestData.EventsSample::remove_B(class [mscorlib]System.EventHandler`1<class [mscorlib]System.EventArgs>)
 }

 .event class [mscorlib]System.Action`1<int32> C {
  .addon instance void CilTools.Tests.Common.TestData.EventsSample::add_C(class [mscorlib]System.Action`1<int32>)
  .removeon instance void CilTools.Tests.Common.TestData.EventsSample::remove_C(class [mscorlib]System.Action`1<int32>)
 }

 //...
}";

            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t);
            string s = Utils.SyntaxToString(nodes);
            AssertThat.CilEquals(expected, s);
        }

        [TestMethod]
        [TypeTestData(typeof(StaticPropertyTest), BytecodeProviders.All)]
        public void Test_StaticProperty(Type t)
        {
            string expected = @".class public auto ansi beforefieldinit CilTools.BytecodeAnalysis.Tests.StaticPropertyTest
extends [mscorlib]System.Object
{
 .property class [CilTools.BytecodeAnalysis.Tests]CilTools.BytecodeAnalysis.Tests.StaticPropertyTest Value()
 {
  .get class CilTools.BytecodeAnalysis.Tests.StaticPropertyTest CilTools.BytecodeAnalysis.Tests.StaticPropertyTest::get_Value()
 }

 //...

}";

            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t);
            string s = Utils.SyntaxToString(nodes);
            AssertThat.CilEquals(expected, s);
        }

        [TestMethod]
        [TypeTestData(typeof(ConstantsTest), BytecodeProviders.All)]
        public void Test_Constants(Type t)
        {
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t);
            string s = Utils.SyntaxToString(nodes);
            
            AssertThat.IsMatch(s, new Text[] {
                ".class", Text.Any,"public", Text.Any,"ConstantsTest", Text.Any,"{", Text.Any,
                ".field public static literal bool Truth = bool(true)", Text.Any,
                ".field public static literal int32 One = int32(1)", Text.Any,
                ".field public static literal string PlanetName = \"Earth\"", Text.Any,
                ".field public static literal object NullValue = nullref", Text.Any,
                "}", Text.Any
            });
        }

        [TestMethod]
        [TypeTestData(typeof(TypeWithProperties), BytecodeProviders.All)]
        public void Test_PropertyAccessor(Type t)
        {
            MethodInfo mi = t.GetProperty("Name").GetGetMethod();
            CilGraph gr = CilGraph.Create(mi);
            string s = gr.ToString();

            string expected = ".method public hidebysig specialname instance string get_Name() cil managed";
            AssertThat.AreLexicallyEqual(expected, s);
        }
    }
}
