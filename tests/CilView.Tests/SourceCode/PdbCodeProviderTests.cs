/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.SourceCode;
using CilTools.Tests.Common;
using CilTools.Tests.Common.Attributes;
using CilView.Core;
using CilView.SourceCode;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilView.Tests.SourceCode
{
    [TestClass]
    public class PdbCodeProviderTests
    {
        const string conditionMessage = "PDB content is different in debug and release";

        static void VerifyFragments(IEnumerable<SourceFragment> fragments, SourceDocument doc, MethodBase mb)
        {
            foreach (SourceFragment fragment in fragments)
            {
                Assert.AreSame(doc, fragment.Document);
                Assert.AreSame(mb, fragment.Method);
            }
        }

        [ConditionalTest(TestCondition.DebugBuildOnly, conditionMessage)]
        public void Test_GetSourceCodeDocuments_Full()
        {
            MethodBase mb = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            IEnumerable<SourceDocument> docs = PdbCodeProvider.Instance.GetSourceCodeDocuments(mb);
            SourceDocument doc = docs.First();

            Assert.AreSame(mb, doc.Method);
            Assert.AreEqual(16, doc.LineStart);
            Assert.AreEqual(18, doc.LineEnd);
            Assert.IsTrue(doc.SymbolsFile.EndsWith(@"CilTools.Tests.Common.pdb"));
            Assert.AreEqual("Portable PDB", doc.SymbolsFileFormat);

            string path = doc.FilePath.Replace('/', '\\');
            Assert.IsTrue(path.EndsWith(@"CilTools\tests\CilTools.Tests.Common\TestData\SampleMethods.cs"));

            string expected = "{ Console.WriteLine(\"Hello, World\"); }";
            AssertThat.AreLexicallyEqual(expected, doc.Text);

            SourceFragment[] fragments = doc.Fragments.ToArray();
            Assert.AreEqual(3, fragments.Length);
            VerifyFragments(fragments, doc, mb);

            Assert.AreEqual(16, fragments[0].LineStart);
            Assert.AreEqual(16, fragments[0].LineEnd);
            AssertThat.AreLexicallyEqual("{", fragments[0].Text);

            Assert.AreEqual(17, fragments[1].LineStart);
            Assert.AreEqual(17, fragments[1].LineEnd);
            AssertThat.AreLexicallyEqual("Console.WriteLine(\"Hello, World\");", fragments[1].Text);
            
            Assert.AreEqual(18, fragments[2].LineStart);
            Assert.AreEqual(18, fragments[2].LineEnd);
            AssertThat.AreLexicallyEqual("}", fragments[2].Text);
        }

        [TestMethod]
        public void Test_GetSourceCodeDocuments_Short()
        {
            MethodBase mb = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            IEnumerable<SourceDocument> docs = PdbCodeProvider.Instance.GetSourceCodeDocuments(mb);
            SourceDocument doc = docs.First();

            Assert.AreSame(mb, doc.Method);
            Assert.IsTrue(doc.SymbolsFile.EndsWith(@"CilTools.Tests.Common.pdb"));
            Assert.AreEqual("Portable PDB", doc.SymbolsFileFormat);

            string path = doc.FilePath.Replace('/', '\\');
            Assert.IsTrue(path.EndsWith(@"CilTools\tests\CilTools.Tests.Common\TestData\SampleMethods.cs"));
            
            Assert.IsTrue(doc.Text.Contains("Console.WriteLine(\"Hello, World\");"));

            SourceFragment[] fragments = doc.Fragments.ToArray();
            Assert.IsTrue(fragments.Length > 0);
            VerifyFragments(fragments, doc, mb);
        }
    }
}
