/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Metadata;
using CilTools.Syntax;
using CilTools.Tests.Common;
using CilTools.Tests.Common.TextUtils;

namespace CilTools.BytecodeAnalysis.Tests.Syntax
{
    [TestClass]
    public class DisassemblerTests
    {
        static void VerifyAssemblyManifest(IEnumerable<SyntaxNode> nodes, string ver)
        {
            //syntax tree
            AssertThat.IsSyntaxTreeCorrect(nodes);

            AssertThat.HasOnlyOneMatch(nodes, (x) =>
            {
                return x is DirectiveSyntax && (x as DirectiveSyntax).Name == "assembly" &&
                    x.ToString().Contains("extern CilTools.BytecodeAnalysis");
            });

            AssertThat.HasOnlyOneMatch(nodes, (x) =>
            {
                return x is DirectiveSyntax && (x as DirectiveSyntax).Name == "assembly" &&
                    x.ToString().Contains("extern CilTools.Metadata");
            });

            AssertThat.HasOnlyOneMatch(nodes, (x) =>
            {
                return x is DirectiveSyntax && (x as DirectiveSyntax).Name == "assembly" &&
                    x.ToString().Contains("CilTools.Tests.Common");
            });

            AssertThat.HasAtLeastOneMatch(nodes, (x) =>
            {
                return x is DirectiveSyntax && (x as DirectiveSyntax).Name == "ver" &&
                    x.ToString().Contains(ver);
            });

            AssertThat.HasOnlyOneMatch(nodes, (x) =>
            {
                return x is DirectiveSyntax && (x as DirectiveSyntax).Name == "module" &&
                    x.ToString().Contains("CilTools.Tests.Common.dll");
            });
        }

        [TestMethod]
        public void Test_GetAssemblyManifestSyntaxNodes_Reflection()
        {
            Assembly ass = typeof(SampleMethods).Assembly;
            string ver = Utils.GetVersionIL(ass);
            IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);
            string str = Utils.SyntaxToString(nodes);

            VerifyAssemblyManifest(nodes, ver);
            SyntaxTestsCore.VerifyAssemblyManifestString(str, ver);
        }

        [TestMethod]
        public void Test_GetAssemblyManifestSyntaxNodes_Metadata()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                string ver = Utils.GetVersionIL(ass);
                IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);
                string str = Utils.SyntaxToString(nodes);

                //syntax tree
                VerifyAssemblyManifest(nodes, ver);

                AssertThat.HasOnlyOneMatch(nodes, (x) =>
                {
                    return x is DirectiveSyntax && (x as DirectiveSyntax).Name == "module" &&
                        x.ToString().ToLower().Contains("extern user32.dll");
                });

                //text output
                SyntaxTestsCore.VerifyAssemblyManifestString(str, ver);

                AssertThat.IsMatch(str.ToLower(), new Text[] {
                    ".module", Text.Any, "extern", Text.Any, "user32.dll"
                });

                AssertThat.IsMatch(str, new Text[] {
                    ".imagebase", Text.Any,
                    ".file", Text.Any, "alignment", Text.Any,
                    ".stackreserve", Text.Any, 
                    ".subsystem", Text.Any, "0x3", Text.Any, "// WINDOWS_CUI", Text.Any,
                    ".corflags", Text.Any, "0x1", Text.Any
                });
            }
        }
    }
}
