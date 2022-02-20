/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using CilTools.BytecodeAnalysis;
using CilTools.BytecodeAnalysis.Tests;
using CilTools.Syntax;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.SourceCode.Tests
{
    [TestClass]
    public class SourceCodeProviderTests
    {
        const string srcstring = "Console.WriteLine(\"Hello, World\");";
        static readonly MockCodeProvider provider = new MockCodeProvider();

        class MockCodeProvider : SourceCodeProvider
        {
            public override IEnumerable<SourceDocument> GetSourceCodeDocuments(MethodBase m)
            {
                SourceDocument doc = new SourceDocument();
                doc.Method = m;

                SourceFragment fragment = new SourceFragment();
                fragment.CilStart = 1;
                fragment.CilEnd = 12;
                fragment.Text = srcstring;
                doc.AddFragment(fragment);

                return new SourceDocument[] { doc };
            }

            public override string GetSignatureSourceCode(MethodBase m)
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_SourceCodeProvider(MethodBase m)
        {
            CilGraph graph = CilGraph.Create(m);

            //with source code
            DisassemblerParams pars = new DisassemblerParams();
            pars.IncludeSourceCode = true;
            pars.CodeProvider = provider;
            MethodDefSyntax syntax=graph.ToSyntaxTree(pars);
            string str = syntax.ToString();

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any, "void", Text.Any, "PrintHelloWorld", Text.Any, "cil", Text.Any, "managed", Text.Any,
                "{", Text.Any,
                "//" + srcstring, Text.Any,
                "call", Text.Any, "void", Text.Any, "Console::WriteLine", Text.Any,
                "ret", Text.Any,
                "}"
            });

            //without source code
            pars = new DisassemblerParams();
            pars.IncludeSourceCode = false;
            pars.CodeProvider = provider;
            syntax = graph.ToSyntaxTree(pars);
            str = syntax.ToString();
            Assert.IsFalse(str.Contains("//" + srcstring));
        }
    }
}
