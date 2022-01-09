/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilTools.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Tests.Common
{
    public static class SyntaxTestsCore
    {
        public static void Test_ToSyntaxTree(MethodBase mi)
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

        public static void Test_KeywordAsIdentifier(MethodBase mi)
        {
            string str = CilAnalysis.MethodToText(mi);

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any, "public", Text.Any,
                "void", Text.Any,
                "'method'", Text.Any,
                "cil", Text.Any, "managed", Text.Any,                
            });
        }

        public static void SampleMethods_AssertTypeSyntax(string s)
        {
            AssertThat.IsMatch(s, new Text[] {
                ".class", Text.Any,"public", Text.Any,"abstract", Text.Any,"CilTools.Tests.Common.SampleMethods", Text.Any,
                "{", Text.Any,
                ".field", Text.Any,"public", Text.Any,"static", Text.Any,"int32", Text.Any,"Foo", Text.Any,
                "}", Text.Any
            });

            AssertThat.IsMatch(s, new Text[] {
                ".class", Text.Any,
                ".field", Text.Any,"public", Text.Any,"static", Text.Any,"int32", Text.Any,"counter", Text.Any
            });

            AssertThat.IsMatch(s, new Text[] {
                ".class", Text.Any,
                ".field", Text.Any,"public", Text.Any,"static", Text.Any,"int32", Text.Any,"f", Text.Any
            });

            Assert.IsFalse(s.Contains(".method"));
        }

        public static void Test_GetTypeDefSyntax_Short(Type t)
        {
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t);
            string s = Utils.SyntaxToString(nodes);
            SampleMethods_AssertTypeSyntax(s);
        }

        public static void Test_GetTypeDefSyntax_Full(Type t)
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
    }
}
