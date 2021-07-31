/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Tests.Common;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class SyntaxTests
    {
        [TestMethod]
        public void Test_ToSyntaxTree()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            SyntaxTestsCore.Test_ToSyntaxTree(mi);
        }

        [TestMethod]
        public void Test_KeywordAsIdentifier()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("method");
            SyntaxTestsCore.Test_KeywordAsIdentifier(mi);
        }
    }
}
