/* CIL Tools
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.SourceCode.Common;
using CilTools.Syntax;
using CilTools.Visualization;

namespace CilView.Tests.Visualization
{
    [TestClass]
    public class HtmlVisualizerTests
    {
        static string Visualize(IEnumerable<SyntaxNode> nodes)
        {
            HtmlVisualizer vis = new HtmlVisualizer();
            return vis.RenderNodes(nodes);
        }

        static string Preformatted(string html)
        {
            return "<pre style=\"white-space: pre-wrap;\"><code>" + html + "</code></pre>";
        }

        [TestMethod]
        public void Test_RenderSyntaxNodes_SourceToken()
        {
            SyntaxNode[] nodes = new SyntaxNode[]
            {
                new SourceToken("string", TokenKind.Keyword, string.Empty, " "),
                new SourceToken("str", TokenKind.Name, string.Empty, " "),
                new SourceToken("=", TokenKind.Punctuation, string.Empty, " "),
                new SourceToken("\"Hello, world!\"", TokenKind.DoubleQuotLiteral, string.Empty, string.Empty),
                new SourceToken(";", TokenKind.Punctuation, string.Empty, " "),
                new SourceToken("//Comment", TokenKind.Comment, string.Empty, string.Empty),
            };

            string expected = Preformatted("<span style=\"color: blue;\">string </span>" +
                "str = <span style=\"color: red;\">&quot;Hello, world!&quot;</span>;" +
                " <span style=\"color: green;\">//Comment</span>");

            string str = Visualize(nodes);
            Assert.AreEqual(expected, str);
        }

        [TestMethod]
        public void Test_RenderSyntaxNodes_SpecialTextLiteral()
        {
            SyntaxNode[] nodes = new SyntaxNode[]
            {
                new SourceToken("string", TokenKind.Keyword, string.Empty, " "),
                new SourceToken("s", TokenKind.Name, string.Empty, " "),
                new SourceToken("=", TokenKind.Punctuation, string.Empty, " "),
                new SourceToken("@\"Quick brown fox\"", TokenKind.SpecialTextLiteral, string.Empty, string.Empty),
                new SourceToken(";", TokenKind.Punctuation, string.Empty, string.Empty),
            };

            string expected = Preformatted("<span style=\"color: blue;\">string </span>" +
                "s = <span style=\"color: red;\">@&quot;Quick brown fox&quot;</span>;");

            string str = Visualize(nodes);
            Assert.AreEqual(expected, str);
        }

        [TestMethod]
        public void Test_RenderSyntaxNodes_MethodSignature()
        {
            SyntaxNode[] nodes = new SyntaxNode[]
            {
                SyntaxFactory.CreateFromToken(".method", string.Empty, " "),
                SyntaxFactory.CreateFromToken("public", string.Empty, " "),
                SyntaxFactory.CreateFromToken("static", string.Empty, " "),
                SyntaxFactory.CreateFromToken("void", string.Empty, " "),
                SyntaxFactory.CreateFromToken("Foo", string.Empty, " "),
                SyntaxFactory.CreateFromToken("(", string.Empty, string.Empty),
                SyntaxFactory.CreateFromToken(")", string.Empty, string.Empty),
            };

            string expected = Preformatted("<span style=\"color: magenta;\">.method </span>" +
                "<span style=\"color: blue;\">public </span><span style=\"color: blue;\">static </span>" +
                "<span style=\"color: blue;\">void </span><span>Foo </span>()");

            string str = Visualize(nodes);
            Assert.AreEqual(expected, str);
        }

        [TestMethod]
        public void Test_RenderSyntaxNodes_StringLiteral()
        {
            SyntaxNode[] nodes = new SyntaxNode[]
            {
                SyntaxFactory.CreateFromToken("ldstr", string.Empty, " "),
                SyntaxFactory.CreateFromToken("\"Rabbits\"", string.Empty, " "),
                SyntaxFactory.CreateFromToken("// Load string", string.Empty, string.Empty),
            };

            string expected = Preformatted("ldstr " +
                "<span style=\"color: red;\">&quot;Rabbits&quot; </span>" +
                "<span style=\"color: green;\">// Load string</span>");

            string str = Visualize(nodes);
            Assert.AreEqual(expected, str);
        }
    }
}
