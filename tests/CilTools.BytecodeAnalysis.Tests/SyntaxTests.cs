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
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_ToSyntaxTree(MethodBase mi)
        {
            SyntaxTestsCore.Test_ToSyntaxTree(mi);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "method", BytecodeProviders.All)]
        public void Test_KeywordAsIdentifier(MethodBase mi)
        {
            SyntaxTestsCore.Test_KeywordAsIdentifier(mi);
        }
    }
}
