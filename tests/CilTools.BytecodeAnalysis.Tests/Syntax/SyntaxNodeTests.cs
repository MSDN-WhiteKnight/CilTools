﻿/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.Syntax;
using CilTools.Tests.Common;
using CilTools.Tests.Common.Attributes;
using CilTools.Tests.Common.TestData;
using CilTools.Tests.Common.TextUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests.Syntax
{
    [TestClass]
    public class SyntaxNodeTests
    {
        [TestMethod]
        [TypeTestData(typeof(SampleMethods), BytecodeProviders.All)]
        public void Test_GetTypeDefSyntax_Short(Type t)
        {
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t);
            string s = Utils.SyntaxToString(nodes);
            SyntaxTestsCore.SampleMethods_AssertTypeSyntax(s);
        }

        [TestMethod]
        [TypeTestData(typeof(DisassemblerSampleType), BytecodeProviders.All)]
        public void Test_GetTypeDefSyntax_Full(Type t)
        {
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, true, new DisassemblerParams());
            string s = Utils.SyntaxToString(nodes);

            AssertThat.IsMatch(s, new Text[] {
                ".class", Text.Any,"public", Text.Any,"CilTools.Tests.Common.DisassemblerSampleType", Text.Any,
                "{", Text.Any,
                ".field", Text.Any,"public", Text.Any,"static", Text.Any,"int32", Text.Any,"x", Text.Any,
                ".method", Text.Any,"static", Text.Any,"Test()", Text.Any,"cil", Text.Any,"managed", Text.Any,
                "{", Text.Any,".maxstack", Text.Any,"8", Text.Any,"ldstr", Text.Any,"\"Hello, World\"", Text.Any,
                "call", Text.Any, "System.Console::WriteLine", Text.Any,"ret", Text.Any,"}", Text.Any,
                "}", Text.Any
            });
        }

        [TestMethod]
        [WorkItem(129)]
        [TypeTestData(typeof(InterfacesSampleType), BytecodeProviders.All)]
        public void Test_GetTypeDefSyntax_Interfaces(Type t)
        {
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, false, new DisassemblerParams());
            string s = Utils.SyntaxToString(nodes);

            const string expected = @"
.class public auto ansi beforefieldinit CilTools.Tests.Common.TestData.InterfacesSampleType
    extends [mscorlib]System.Object
    implements [CilTools.Tests.Common]CilTools.Tests.Common.TestData.ITest,
               [mscorlib]System.IComparable { 
//... }";

            AssertThat.CilEquals(expected, s);
        }

        [TestMethod]
        [WorkItem(129)]
        [TypeTestData(typeof(DisassemblerSampleType), BytecodeProviders.Reflection)]
        public void Test_GetTypeDefSyntax_BaseType_Object(Type t)
        {
            string corelib = typeof(object).Assembly.GetName().Name;
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, false, new DisassemblerParams());
            string s = Utils.SyntaxToString(nodes);
            Assert.IsTrue(s.Contains("extends ["+ corelib + "]System.Object"));
        }

        [TestMethod]
        [WorkItem(129)]
        [TypeTestData(typeof(DerivedSampleType), BytecodeProviders.All)]
        public void Test_GetTypeDefSyntax_BaseType(Type t)
        {
            const string expected = @"
.class public auto ansi beforefieldinit CilTools.Tests.Common.DerivedSampleType
extends [CilTools.Tests.Common]CilTools.Tests.Common.DisassemblerSampleType { 
    //...
}";
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, false, new DisassemblerParams());
            string s = Utils.SyntaxToString(nodes);
            AssertThat.CilEquals(expected, s);
        }

        [TestMethod]
        [TypeTestData(typeof(ITest), BytecodeProviders.All)]
        public void Test_GetTypeDefSyntax_Interface(Type t)
        {
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, false, new DisassemblerParams());
            string s = Utils.SyntaxToString(nodes);
            Assert.IsFalse(s.Contains("extends"));
        }
    }
}
