/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
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
        public string this[int i] { get { return i.ToString(); } }
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

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "Sum", BytecodeProviders.All)]
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
    }
}
