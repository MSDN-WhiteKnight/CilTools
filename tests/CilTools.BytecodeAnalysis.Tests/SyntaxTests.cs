/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Syntax;
using CilTools.Tests.Common;

namespace CilTools.BytecodeAnalysis.Tests
{
    public class TypeWithProperties
    {
        public string Name { get; set; }
        public int Number { get; }
    }

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

        [TestMethod]        
        public void Test_Properties()
        {
            IEnumerable<SyntaxNode> nodes=SyntaxNode.GetTypeDefSyntax(typeof(TypeWithProperties));
            StringBuilder sb = new StringBuilder();
            StringWriter wr = new StringWriter(sb);

            foreach (SyntaxNode node in nodes) 
            {
                node.ToText(wr);
            }

            string s = sb.ToString();
                        
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
        }
    }
}
