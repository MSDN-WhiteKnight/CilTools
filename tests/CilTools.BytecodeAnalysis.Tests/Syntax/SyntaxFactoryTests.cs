/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests.Syntax
{
    [TestClass]
    public class SyntaxFactoryTests
    {
        [TestMethod]
        public void Test_CreateFromToken()
        {
            string token = "Foo";
            string lead = " ";
            string trail = Environment.NewLine;

            SyntaxNode node = SyntaxFactory.CreateFromToken(token, lead, trail);
            Assert.IsTrue(node is IdentifierSyntax);
            Assert.AreEqual(lead, node.LeadingWhitespace);
            Assert.AreEqual(trail, node.TrailingWhitespace);
            Assert.AreEqual(" Foo" + Environment.NewLine, node.ToString());
            Assert.AreEqual(token, (node as IdentifierSyntax).Content);
        }

        [DataTestMethod]
        [DataRow(".method", KeywordKind.DirectiveName)]
        [DataRow("ldstr", KeywordKind.InstructionName)]
        [DataRow("string", KeywordKind.Other)]
        public void Test_CreateFromToken_Keyword(string token, KeywordKind expectedKind)
        {
            SyntaxNode node = SyntaxFactory.CreateFromToken(token, string.Empty, string.Empty);
            Assert.IsTrue(node is KeywordSyntax);
            Assert.AreEqual(token, node.ToString());
            Assert.AreEqual(token, (node as KeywordSyntax).Content);
            Assert.AreEqual(expectedKind, (node as KeywordSyntax).Kind);
        }

        [DataTestMethod]
        [DataRow("\"Quick brown fox\"")]
        [DataRow(@"""\\\""""")]
        [DataRow("\"//Some comment inside string\"")]
        [DataRow("12")]
        [DataRow("5.6789")]
        public void Test_CreateFromToken_Literal(string token)
        {
            SyntaxNode node = SyntaxFactory.CreateFromToken(token, string.Empty, string.Empty);
            Assert.IsTrue(node is LiteralSyntax);
            Assert.AreEqual(token, node.ToString());
        }

        [TestMethod]
        public void Test_CreateFromToken_StringLiteral()
        {
            string token = "\"'Hello world'\"";
            SyntaxNode node = SyntaxFactory.CreateFromToken(token, string.Empty, string.Empty);
            Assert.IsTrue(node is LiteralSyntax);
            Assert.AreEqual("'Hello world'", (node as LiteralSyntax).Value);
            Assert.AreEqual(token, node.ToString());
        }

        [DataTestMethod]
        [DataRow("_Foo")]
        [DataRow("x1")]
        [DataRow("WriteLine")]
        [DataRow("'ldstr'")]
        [DataRow("'\"Quick brown fox\"'")]
        [DataRow("$abc@")]
        public void Test_CreateFromToken_Identifier(string token)
        {
            SyntaxNode node = SyntaxFactory.CreateFromToken(token, string.Empty, string.Empty);
            Assert.IsTrue(node is IdentifierSyntax);
            Assert.AreEqual(token, node.ToString());
        }

        [DataTestMethod]
        [DataRow("//Foo")]
        [DataRow("/*Bar*/")]
        [DataRow("//")]
        [DataRow("/**/")]
        public void Test_CreateFromToken_Comment(string token)
        {
            SyntaxNode node = SyntaxFactory.CreateFromToken(token, string.Empty, string.Empty);
            Assert.IsTrue(node is CommentSyntax);
            Assert.AreEqual(token, node.ToString());
        }

        [TestMethod]
        public void Test_CreateFromToken_Invalid()
        {
            string token;
            SyntaxNode node;

            token = "\"Hello world";
            node = SyntaxFactory.CreateFromToken(token, string.Empty, string.Empty);
            Assert.IsFalse(node is LiteralSyntax);
            Assert.AreEqual(token, node.ToString());

            token = "'method";
            node = SyntaxFactory.CreateFromToken(token, string.Empty, string.Empty);
            Assert.IsFalse(node is IdentifierSyntax);
            Assert.AreEqual(token, node.ToString());

            token = "/*comment";
            node = SyntaxFactory.CreateFromToken(token, string.Empty, string.Empty);
            Assert.IsFalse(node is CommentSyntax);
            Assert.AreEqual(token, node.ToString());
        }
    }
}
