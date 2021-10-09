/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class IlAsmTests
    {
        [TestMethod]
        public void Test_IndirectCall_IlAsm()
        {
            const string code = @"
.method public static void IndirectCallTest(string x) cil managed 
{ 
 .maxstack   2

          ldarg.0      
          ldftn        void [mscorlib]System.Console::WriteLine(string)
          calli        void (string)
          ret          
}";
            //compile method from CIL
            MethodBase mb = IlAsm.BuildFunction(code, "IndirectCallTest");
            mb.Invoke(null, new object[] { "Hello from CIL Tools tests!" });

            //create CilGraph from method
            CilGraph graph = CilGraph.Create(mb);

            //verify CilGraph
            AssertThat.IsCorrect(graph);
        }
    }
}
