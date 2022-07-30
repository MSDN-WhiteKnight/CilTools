/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.Syntax;
using CilTools.Tests.Common;
using CilTools.Tests.Common.TestData;
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
        public void Test_GetTypeDefSyntax_Interfaces()
        {
            SyntaxTestsCore.Test_GetTypeDefSyntax_Interfaces(typeof(InterfacesSampleType));
        }
    }
}
