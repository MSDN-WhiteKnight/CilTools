/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Tests.Common;
using CilView.SourceCode;

namespace CilView.Tests.SourceCode
{
    [TestClass]
    public class TokenClassifierTests
    {
        [TestMethod]
        [DataRow("class", SourceTokenKind.Keyword)]
        [DataRow("Foo", SourceTokenKind.OtherName)]
        [DataRow("1", SourceTokenKind.NumericLiteral)]
        [DataRow("5.6", SourceTokenKind.NumericLiteral)]
        [DataRow(";", SourceTokenKind.Punctuation)]
        [DataRow("/", SourceTokenKind.Punctuation)]
        [DataRow("\"String literal\"", SourceTokenKind.StringLiteral)]
        [DataRow("//Comment", SourceTokenKind.Comment)]
        [DataRow("/*Another comment*/", SourceTokenKind.Comment)]
        [DataRow("array", SourceTokenKind.OtherName)]
        [DataRow("_var", SourceTokenKind.OtherName)]
        [DataRow("\"//Comment\"", SourceTokenKind.StringLiteral)]
        public void Test_CsharpClassifier(string token, SourceTokenKind expected)
        {
            CsharpClassifier classifier = new CsharpClassifier();
            SourceTokenKind actual = classifier.GetKind(token);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow("class", SourceTokenKind.Keyword)]
        [DataRow("Foo", SourceTokenKind.OtherName)]
        [DataRow("1", SourceTokenKind.NumericLiteral)]
        [DataRow("5.6", SourceTokenKind.NumericLiteral)]
        [DataRow(";", SourceTokenKind.Punctuation)]
        [DataRow("/", SourceTokenKind.Punctuation)]
        [DataRow("\"String literal\"", SourceTokenKind.StringLiteral)]
        [DataRow("//Comment", SourceTokenKind.Comment)]
        [DataRow("/*Another comment*/", SourceTokenKind.Comment)]
        [DataRow("array", SourceTokenKind.Keyword)]
        [DataRow("_var", SourceTokenKind.OtherName)]
        [DataRow("\"//Comment\"", SourceTokenKind.StringLiteral)]
        public void Test_CppClassifier(string token, SourceTokenKind expected)
        {
            CppClassifier classifier = new CppClassifier();
            SourceTokenKind actual = classifier.GetKind(token);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Test_TokenClassifier_Create()
        {
            Assert.IsTrue(TokenClassifier.Create(".cs") is CsharpClassifier);
            Assert.IsTrue(TokenClassifier.Create(".cpp") is CppClassifier);
            Assert.IsTrue(TokenClassifier.Create(".text") is CsharpClassifier);
        }
    }
}
