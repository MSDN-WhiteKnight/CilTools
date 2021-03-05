/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
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
    }
}
