/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CilTools.Syntax;
using CilView.Core.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilView.Tests
{
    [TestClass]
    public class SyntaxReaderTests
    {
        [TestMethod]
        public void Test_SyntaxReader_Call()
        {
            string s = "call       void [mscorlib]System.Console::WriteLine(string)";
            SyntaxNode[] nodes = SyntaxReader.ReadAllNodes(s);
            Assert.AreEqual(12, nodes.Length);

            SyntaxNode node = nodes[0];
            Assert.IsTrue(node is KeywordSyntax);
            Assert.AreEqual("call", (node as KeywordSyntax).Content);
            Assert.AreEqual(KeywordKind.InstructionName, (node as KeywordSyntax).Kind);
            Assert.AreEqual(string.Empty, node.LeadingWhitespace);
            Assert.AreEqual("       ", node.TrailingWhitespace);
            Assert.AreEqual("call       ", node.ToString());

            node = nodes[1];
            Assert.IsTrue(node is KeywordSyntax);
            Assert.AreEqual("void", (node as KeywordSyntax).Content);
            Assert.AreEqual(KeywordKind.Other, (node as KeywordSyntax).Kind);
            Assert.AreEqual("void ", node.ToString());

            node = nodes[2];
            Assert.AreEqual(string.Empty, node.LeadingWhitespace);
            Assert.AreEqual(string.Empty, node.TrailingWhitespace);
            Assert.AreEqual("[", node.ToString());

            node = nodes[3];
            Assert.IsTrue(node is IdentifierSyntax);
            Assert.AreEqual("mscorlib", (node as IdentifierSyntax).Content);
            Assert.AreEqual("mscorlib", node.ToString());

            node = nodes[4];
            Assert.AreEqual(string.Empty, node.LeadingWhitespace);
            Assert.AreEqual(string.Empty, node.TrailingWhitespace);
            Assert.AreEqual("]", node.ToString());

            node = nodes[5];
            Assert.IsTrue(node is IdentifierSyntax);
            Assert.AreEqual("System.Console", (node as IdentifierSyntax).Content);
            Assert.AreEqual("System.Console", node.ToString());

            node = nodes[6];
            Assert.AreEqual(string.Empty, node.LeadingWhitespace);
            Assert.AreEqual(string.Empty, node.TrailingWhitespace);
            Assert.AreEqual(":", node.ToString());

            node = nodes[7];
            Assert.AreEqual(string.Empty, node.LeadingWhitespace);
            Assert.AreEqual(string.Empty, node.TrailingWhitespace);
            Assert.AreEqual(":", node.ToString());

            node = nodes[8];
            Assert.IsTrue(node is IdentifierSyntax);
            Assert.AreEqual("WriteLine", (node as IdentifierSyntax).Content);
            Assert.AreEqual("WriteLine", node.ToString());

            node = nodes[9];
            Assert.AreEqual(string.Empty, node.LeadingWhitespace);
            Assert.AreEqual(string.Empty, node.TrailingWhitespace);
            Assert.AreEqual("(", node.ToString());

            node = nodes[10];
            Assert.IsTrue(node is KeywordSyntax);
            Assert.AreEqual("string", (node as KeywordSyntax).Content);
            Assert.AreEqual(KeywordKind.Other, (node as KeywordSyntax).Kind);
            Assert.AreEqual("string", node.ToString());

            node = nodes[11];
            Assert.AreEqual(string.Empty, node.LeadingWhitespace);
            Assert.AreEqual(string.Empty, node.TrailingWhitespace);
            Assert.AreEqual(")", node.ToString());
        }

        [TestMethod]
        public void Test_SyntaxReader_Literals()
        {
            string s = " ldstr \"Hello, World\" //load string\r\nldc.i4 10 /*load integer*/";
            SyntaxNode[] nodes = SyntaxReader.ReadAllNodes(s);
            Assert.AreEqual(6, nodes.Length);

            SyntaxNode node = nodes[0];
            Assert.IsTrue(node is KeywordSyntax);
            Assert.AreEqual("ldstr", (node as KeywordSyntax).Content);
            Assert.AreEqual(KeywordKind.InstructionName, (node as KeywordSyntax).Kind);
            Assert.AreEqual(" ", node.LeadingWhitespace);
            Assert.AreEqual(" ", node.TrailingWhitespace);
            Assert.AreEqual(" ldstr ", node.ToString());

            node = nodes[1];
            Assert.IsTrue(node is LiteralSyntax);
            Assert.AreEqual(string.Empty, node.LeadingWhitespace);
            Assert.AreEqual(" ", node.TrailingWhitespace);
            Assert.AreEqual("Hello, World", (node as LiteralSyntax).Value);
            Assert.AreEqual("\"Hello, World\" ", node.ToString());

            node = nodes[2];
            Assert.IsTrue(node is CommentSyntax);
            Assert.AreEqual(string.Empty, node.LeadingWhitespace);
            Assert.AreEqual("\r\n", node.TrailingWhitespace);
            Assert.AreEqual("//load string\r\n", node.ToString());

            node = nodes[3];
            Assert.IsTrue(node is KeywordSyntax);
            Assert.AreEqual("ldc.i4", (node as KeywordSyntax).Content);
            Assert.AreEqual(KeywordKind.InstructionName, (node as KeywordSyntax).Kind);
            Assert.AreEqual(string.Empty, node.LeadingWhitespace);
            Assert.AreEqual(" ", node.TrailingWhitespace);
            Assert.AreEqual("ldc.i4 ", node.ToString());

            node = nodes[4];
            Assert.AreEqual(string.Empty, node.LeadingWhitespace);
            Assert.AreEqual(" ", node.TrailingWhitespace);
            Assert.AreEqual("10 ", node.ToString());

            node = nodes[5];
            Assert.IsTrue(node is CommentSyntax);
            Assert.AreEqual(string.Empty, node.LeadingWhitespace);
            Assert.AreEqual(string.Empty, node.TrailingWhitespace);
            Assert.AreEqual("/*load integer*/", node.ToString());
        }

        [TestMethod]
        public void Test_SyntaxReader_Directive()
        {
            string s = ".method public static void 'method'()";
            SyntaxNode[] nodes = SyntaxReader.ReadAllNodes(s);
            Assert.AreEqual(7, nodes.Length);

            SyntaxNode node = nodes[0];
            Assert.IsTrue(node is KeywordSyntax);
            Assert.AreEqual(".method", (node as KeywordSyntax).Content);
            Assert.AreEqual(KeywordKind.DirectiveName, (node as KeywordSyntax).Kind);
            Assert.AreEqual(string.Empty, node.LeadingWhitespace);
            Assert.AreEqual(" ", node.TrailingWhitespace);
            Assert.AreEqual(".method ", node.ToString());

            node = nodes[4];
            Assert.IsTrue(node is IdentifierSyntax);
            Assert.AreEqual(string.Empty, node.LeadingWhitespace);
            Assert.AreEqual(string.Empty, node.TrailingWhitespace);
            Assert.AreEqual("'method'", node.ToString());
        }

        [DataTestMethod]
        [DataRow("call       void [mscorlib]System.Console::WriteLine(string)")]
        [DataRow("ldstr \"Hello, World\"")]
        [DataRow("int i=1*2/0.5; /*число*/ string s1 = \"Hello, world\";/*string2*/ char c='\\'';")]
        [DataRow(@"string s1=""\"""";string s2=""\\"";char c1='\'';char c2='\\';Foo();")]
        [DataRow("IL_0001: ldc.i4.s 10")]
        [DataRow(".method public hidebysig static void 'method'() cil managed")]
        [DataRow("ldc.i4.1 //load integer value onto the stack\r\nadd")]
        [DataRow(TokenReaderTests.Data_MultilineString, DisplayName = "Test_SyntaxReader_Roundtrip(Data_MultilineString)")]
        public void Test_SyntaxReader_Roundtrip(string src)
        {
            SyntaxNode[] nodes = SyntaxReader.ReadAllNodes(src);
            StringBuilder sb = new StringBuilder(src.Length);
            StringWriter wr = new StringWriter(sb);

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].ToText(wr);
            }

            wr.Flush();
            string processed = sb.ToString();
            Assert.AreEqual(src, processed);
        }
    }
}
