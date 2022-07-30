/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.Syntax;
using CilTools.Tests.Common;
using CilTools.Tests.Common.TestData;
using CilTools.Tests.Common.TextUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests.Syntax
{
    [TestClass]
    public class SyntaxNodeTests
    {
        [TestMethod]
        public void Test_GetTypeDefSyntax_Short()
        {
            SyntaxTestsCore.Test_GetTypeDefSyntax_Short(typeof(SampleMethods));
        }

        [TestMethod]
        public void Test_GetTypeDefSyntax_Full()
        {
            SyntaxTestsCore.Test_GetTypeDefSyntax_Full(typeof(DisassemblerSampleType));
        }

        [TestMethod]
        [WorkItem(129)]
        public void Test_GetTypeDefSyntax_Interfaces()
        {
            SyntaxTestsCore.Test_GetTypeDefSyntax_Interfaces(typeof(InterfacesSampleType));
        }

        [TestMethod]
        [WorkItem(129)]
        public void Test_GetTypeDefSyntax_BaseType_Object()
        {
            Type t = typeof(DisassemblerSampleType);
            string corelib = typeof(object).Assembly.GetName().Name;
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, false, new DisassemblerParams());
            string s = Utils.SyntaxToString(nodes);
            Assert.IsTrue(s.Contains("extends ["+ corelib + "]System.Object"));
        }

        [TestMethod]
        [WorkItem(129)]
        public void Test_GetTypeDefSyntax_BaseType()
        {
            const string expected = @"
.class public auto ansi beforefieldinit CilTools.Tests.Common.DerivedSampleType
extends [CilTools.Tests.Common]CilTools.Tests.Common.DisassemblerSampleType { 
    //...
}";
            Type t = typeof(DerivedSampleType);
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, false, new DisassemblerParams());
            string s = Utils.SyntaxToString(nodes);
            AssertThat.CilEquals(expected, s);
        }

        public void Test_GetTypeDefSyntax_Interface()
        {
            Type t = typeof(ITest);
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, false, new DisassemblerParams());
            string s = Utils.SyntaxToString(nodes);
            Assert.IsFalse(s.Contains("extends"));
        }
    }
}
