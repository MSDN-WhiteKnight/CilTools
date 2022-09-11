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

            //text output
            string s = Utils.SyntaxToString(nodes);

            AssertThat.IsMatch(s, new Text[] {
                ".assembly", Text.Any, "extern", Text.Any, "CilTools.BytecodeAnalysis", Text.Any,
                "{", Text.Any, ".ver", Text.Any, ver, Text.Any, "}", Text.Any,
                ".assembly", Text.Any, "CilTools.Tests.Common", Text.Any, "{", Text.Any,
                ".custom", Text.Any, "System.Reflection.AssemblyTitleAttribute::.ctor(string)", Text.Any,
                ".ver", Text.Any, ver, Text.Any, "}", Text.Any
            });
        }

        [TestMethod]
        public void Test_GetAssemblyManifestSyntaxNodes_Reflection()
        {
            Assembly ass = typeof(SampleMethods).Assembly;
            string ver = ass.GetName().Version.ToString(4).Replace(".", ":");
            IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);

            VerifyAssemblyManifest(nodes, ver);
        }

        [TestMethod]
        public void Test_GetAssemblyManifestSyntaxNodes_Metadata()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                string ver = ass.GetName().Version.ToString(4).Replace(".", ":");
                IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);

                VerifyAssemblyManifest(nodes, ver);

                AssertThat.HasOnlyOneMatch(nodes, (x) =>
                {
                    return x is DirectiveSyntax && (x as DirectiveSyntax).Name == "module" &&
                        x.ToString().ToLower().Contains("extern user32.dll");
                });

                string s = Utils.SyntaxToString(nodes).ToLower();

                AssertThat.IsMatch(s, new Text[] {
                    ".module", Text.Any, "extern", Text.Any, "user32.dll"
                });
            }
        }
    }
}
