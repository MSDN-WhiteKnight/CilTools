/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Syntax;
using CilTools.Tests.Common;
using CilView.SourceCode;
using CilView.SourceCode.VisualBasic;

namespace CilView.Tests.SourceCode
{
    [TestClass]
    public class TokenClassifierTests
    {
        [TestMethod]
        [DataRow("class", TokenKind.Keyword)]
        [DataRow("Foo", TokenKind.Name)]
        [DataRow("1", TokenKind.NumericLiteral)]
        [DataRow("5.6", TokenKind.NumericLiteral)]
        [DataRow(";", TokenKind.Punctuation)]
        [DataRow("/", TokenKind.Punctuation)]
        [DataRow("\"String literal\"", TokenKind.DoubleQuotLiteral)]
        [DataRow("//Comment", TokenKind.Comment)]
        [DataRow("/*Another comment*/", TokenKind.MultilineComment)]
        [DataRow("array", TokenKind.Name)]
        [DataRow("_var", TokenKind.Name)]
        [DataRow("\"//Comment\"", TokenKind.DoubleQuotLiteral)]
        public void Test_CsharpClassifier(string token, TokenKind expected)
        {
            CsharpClassifier classifier = new CsharpClassifier();
            TokenKind actual = classifier.GetKind(token);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow("class", TokenKind.Keyword)]
        [DataRow("Foo", TokenKind.Name)]
        [DataRow("1", TokenKind.NumericLiteral)]
        [DataRow("5.6", TokenKind.NumericLiteral)]
        [DataRow(";", TokenKind.Punctuation)]
        [DataRow("/", TokenKind.Punctuation)]
        [DataRow("\"String literal\"", TokenKind.DoubleQuotLiteral)]
        [DataRow("//Comment", TokenKind.Comment)]
        [DataRow("/*Another comment*/", TokenKind.MultilineComment)]
        [DataRow("array", TokenKind.Keyword)]
        [DataRow("_var", TokenKind.Name)]
        [DataRow("\"//Comment\"", TokenKind.DoubleQuotLiteral)]
        public void Test_CppClassifier(string token, TokenKind expected)
        {
            CppClassifier classifier = new CppClassifier();
            TokenKind actual = classifier.GetKind(token);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow("class", TokenKind.Keyword)]
        [DataRow("Foo", TokenKind.Name)]
        [DataRow("1", TokenKind.NumericLiteral)]
        [DataRow("5.6", TokenKind.NumericLiteral)]
        [DataRow(";", TokenKind.Punctuation)]
        [DataRow("/", TokenKind.Punctuation)]
        [DataRow("\"String literal\"", TokenKind.DoubleQuotLiteral)]
        [DataRow("'Comment", TokenKind.Comment)]
        [DataRow("array", TokenKind.Name)]
        [DataRow("_var", TokenKind.Name)]
        [DataRow("\"//Comment\"", TokenKind.DoubleQuotLiteral)]
        [DataRow("If", TokenKind.Keyword)]
        [DataRow("FOR", TokenKind.Keyword)]
        [DataRow("#If", TokenKind.Keyword)]
        public void Test_VbClassifier(string token, TokenKind expected)
        {
            VbClassifier classifier = new VbClassifier();
            TokenKind actual = classifier.GetKind(token);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Test_TokenClassifier_Create()
        {
            Assert.IsTrue(TokenClassifier.Create(".cs") is CsharpClassifier);
            Assert.IsTrue(TokenClassifier.Create(".vb") is VbClassifier);
            Assert.IsTrue(TokenClassifier.Create(".cpp") is CppClassifier);
            Assert.IsTrue(TokenClassifier.Create(".text") is CsharpClassifier);
        }
    }
}
