/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Tests.Common;
using CilView.SourceCode;
using CilView.Core.Syntax;

namespace CilView.Tests.SourceCode
{
    [TestClass]
    public class SourceTokenTests
    {
        [TestMethod]
        public void Test_ParseTokens_Class()
        {
            string src = "public static class Foo { } /*Fizz Buzz*/";
            SourceToken[] tokens = SourceToken.ParseTokens(src, new CsharpClassifier());
            Assert.AreEqual(7, tokens.Length);

            Assert.AreEqual(TokenKind.Keyword, tokens[0].Kind);
            Assert.AreEqual("public", tokens[0].Content);
            Assert.AreEqual(string.Empty, tokens[0].LeadingWhitespace);
            Assert.AreEqual(" ", tokens[0].TrailingWhitespace);
            Assert.AreEqual("public ", tokens[0].ToString());

            Assert.AreEqual(TokenKind.Keyword, tokens[1].Kind);
            Assert.AreEqual("static", tokens[1].Content);
            Assert.AreEqual(string.Empty, tokens[1].LeadingWhitespace);
            Assert.AreEqual(" ", tokens[1].TrailingWhitespace);
            Assert.AreEqual("static ", tokens[1].ToString());

            Assert.AreEqual(TokenKind.Keyword, tokens[2].Kind);
            Assert.AreEqual("class", tokens[2].Content);
            Assert.AreEqual(string.Empty, tokens[2].LeadingWhitespace);
            Assert.AreEqual(" ", tokens[2].TrailingWhitespace);
            Assert.AreEqual("class ", tokens[2].ToString());

            Assert.AreEqual(TokenKind.Name, tokens[3].Kind);
            Assert.AreEqual("Foo", tokens[3].Content);
            Assert.AreEqual(string.Empty, tokens[3].LeadingWhitespace);
            Assert.AreEqual(" ", tokens[3].TrailingWhitespace);
            Assert.AreEqual("Foo ", tokens[3].ToString());

            Assert.AreEqual(TokenKind.Punctuation, tokens[4].Kind);
            Assert.AreEqual("{", tokens[4].Content);
            Assert.AreEqual(string.Empty, tokens[4].LeadingWhitespace);
            Assert.AreEqual(" ", tokens[4].TrailingWhitespace);
            Assert.AreEqual("{ ", tokens[4].ToString());

            Assert.AreEqual(TokenKind.Punctuation, tokens[5].Kind);
            Assert.AreEqual("}", tokens[5].Content);
            Assert.AreEqual(string.Empty, tokens[5].LeadingWhitespace);
            Assert.AreEqual(" ", tokens[5].TrailingWhitespace);
            Assert.AreEqual("} ", tokens[5].ToString());

            Assert.AreEqual(TokenKind.MultilineComment, tokens[6].Kind);
            Assert.AreEqual("/*Fizz Buzz*/", tokens[6].Content);
            Assert.AreEqual(string.Empty, tokens[6].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[6].TrailingWhitespace);
            Assert.AreEqual("/*Fizz Buzz*/", tokens[6].ToString());
        }

        [TestMethod]
        public void Test_ParseTokens_NumericLiteral()
        {
            string src = "int x=1; float y=2.3; // Comment";
            SourceToken[] tokens = SourceToken.ParseTokens(src, new CsharpClassifier());
            Assert.AreEqual(11, tokens.Length);

            Assert.AreEqual(TokenKind.Keyword, tokens[0].Kind);
            Assert.AreEqual("int", tokens[0].Content);
            Assert.AreEqual(string.Empty, tokens[0].LeadingWhitespace);
            Assert.AreEqual(" ", tokens[0].TrailingWhitespace);
            Assert.AreEqual("int ", tokens[0].ToString());

            Assert.AreEqual(TokenKind.Name, tokens[1].Kind);
            Assert.AreEqual("x", tokens[1].Content);
            Assert.AreEqual(string.Empty, tokens[1].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[1].TrailingWhitespace);
            Assert.AreEqual("x", tokens[1].ToString());

            Assert.AreEqual(TokenKind.Punctuation, tokens[2].Kind);
            Assert.AreEqual("=", tokens[2].Content);
            Assert.AreEqual(string.Empty, tokens[2].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[2].TrailingWhitespace);
            Assert.AreEqual("=", tokens[2].ToString());

            Assert.AreEqual(TokenKind.NumericLiteral, tokens[3].Kind);
            Assert.AreEqual("1", tokens[3].Content);
            Assert.AreEqual(string.Empty, tokens[3].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[3].TrailingWhitespace);
            Assert.AreEqual("1", tokens[3].ToString());

            Assert.AreEqual(TokenKind.Punctuation, tokens[4].Kind);
            Assert.AreEqual(";", tokens[4].Content);
            Assert.AreEqual(string.Empty, tokens[4].LeadingWhitespace);
            Assert.AreEqual(" ", tokens[4].TrailingWhitespace);
            Assert.AreEqual("; ", tokens[4].ToString());

            Assert.AreEqual(TokenKind.Keyword, tokens[5].Kind);
            Assert.AreEqual("float", tokens[5].Content);
            Assert.AreEqual(string.Empty, tokens[5].LeadingWhitespace);
            Assert.AreEqual(" ", tokens[5].TrailingWhitespace);
            Assert.AreEqual("float ", tokens[5].ToString());

            Assert.AreEqual(TokenKind.Name, tokens[6].Kind);
            Assert.AreEqual("y", tokens[6].Content);
            Assert.AreEqual(string.Empty, tokens[6].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[6].TrailingWhitespace);
            Assert.AreEqual("y", tokens[6].ToString());

            Assert.AreEqual(TokenKind.Punctuation, tokens[7].Kind);
            Assert.AreEqual("=", tokens[7].Content);
            Assert.AreEqual(string.Empty, tokens[7].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[7].TrailingWhitespace);
            Assert.AreEqual("=", tokens[7].ToString());

            Assert.AreEqual(TokenKind.NumericLiteral, tokens[8].Kind);
            Assert.AreEqual("2.3", tokens[8].Content);
            Assert.AreEqual(string.Empty, tokens[8].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[8].TrailingWhitespace);
            Assert.AreEqual("2.3", tokens[8].ToString());

            Assert.AreEqual(TokenKind.Punctuation, tokens[9].Kind);
            Assert.AreEqual(";", tokens[9].Content);
            Assert.AreEqual(string.Empty, tokens[9].LeadingWhitespace);
            Assert.AreEqual(" ", tokens[9].TrailingWhitespace);
            Assert.AreEqual("; ", tokens[9].ToString());

            Assert.AreEqual(TokenKind.Comment, tokens[10].Kind);
            Assert.AreEqual("// Comment", tokens[10].Content);
            Assert.AreEqual(string.Empty, tokens[10].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[10].TrailingWhitespace);
            Assert.AreEqual("// Comment", tokens[10].ToString());
        }

        [TestMethod]
        [DataRow("public static class Foo { } /*Fizz Buzz*/")]
        [DataRow("int x=1; float y=2.3; // Comment")]
        [DataRow("int i=1*2/0.5; /*число*/ string s1 = \"Hello, world\";/*string2*/ char c='\\'';")]
        [DataRow(@"string s1=""\"""";string s2=""\\"";char c1='\'';char c2='\\';Foo();")]
        [DataRow("Bar(\"//Comment\");")]
        public void Test_ParseTokens_Roundtrip(string src)
        {
            //c#
            SourceToken[] tokens = SourceToken.ParseTokens(src, new CsharpClassifier());
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < tokens.Length; i++)
            {
                sb.Append(tokens[i].ToString());
            }

            Assert.AreEqual(src, sb.ToString(), "Parsed C# source does not match input string");

            //c++
            tokens = SourceToken.ParseTokens(src, new CppClassifier());
            sb = new StringBuilder();

            for (int i = 0; i < tokens.Length; i++)
            {
                sb.Append(tokens[i].ToString());
            }

            Assert.AreEqual(src, sb.ToString(), "Parsed C++ source does not match input string");
        }
    }
}
