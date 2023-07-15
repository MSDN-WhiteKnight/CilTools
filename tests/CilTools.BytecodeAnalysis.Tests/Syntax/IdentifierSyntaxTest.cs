/* CIL Tools
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Reflection;
using CilTools.Syntax;
using CilTools.Tests.Common;
using CilTools.Tests.Common.Attributes;

namespace CilTools.BytecodeAnalysis.Tests.Syntax
{
    [TestClass]
    public class IdentifierSyntaxTest
    {
        const string ConditionMsg = "Codegen is different in release build";

        [ConditionalTest(TestCondition.DebugBuildOnly, ConditionMsg)]
        [MethodTestData(typeof(SampleMethods), "CreatePoint", BytecodeProviders.All)]
        public void Test_IdentifierSyntax_Locals(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            MethodDefSyntax mds = graph.ToSyntaxTree();
            int c = 0;

            AssertThat.IsSyntaxTreeCorrect(mds);

            Utils.VisitSyntaxTree(mds, (x) => {
                if (!(x is IdentifierSyntax)) return;

                IdentifierSyntax id = (IdentifierSyntax)x;

                if (id.Kind == IdentifierKind.LocalVariable && id.IsDefinition)
                {
                    c++;
                    Assert.IsTrue(id.TargetItem is LocalVariable);
                }
            });

            Assert.IsTrue(c > 0);
        }
    }
}
