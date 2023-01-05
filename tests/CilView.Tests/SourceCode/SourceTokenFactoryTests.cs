/* CIL Tools
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.SourceCode.Common;
using CilTools.Syntax;
using CilTools.Tests.Common;

namespace CilView.Tests.SourceCode
{
    [TestClass]
    public class SourceTokenFactoryTests
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
        public void Test_Csharp(string token, TokenKind expected)
        {
            SourceTokenFactory factory = SourceCodeUtils.GetFactory(SourceLanguage.CSharp);
            SourceToken st = (SourceToken)factory.CreateNode(token, string.Empty, string.Empty);
            Assert.AreEqual(expected, st.Kind);
            Assert.AreEqual(token, st.Content);
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
        public void Test_Cpp(string token, TokenKind expected)
        {
            SourceTokenFactory factory = SourceCodeUtils.GetFactory(SourceLanguage.Cpp);
            SourceToken st = (SourceToken)factory.CreateNode(token, string.Empty, string.Empty);
            Assert.AreEqual(expected, st.Kind);
            Assert.AreEqual(token, st.Content);
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
        public void Test_VisualBasic(string token, TokenKind expected)
        {
            SourceTokenFactory factory = SourceCodeUtils.GetFactory(SourceLanguage.VisualBasic);
            SourceToken st = (SourceToken)factory.CreateNode(token, string.Empty, string.Empty);
            Assert.AreEqual(expected, st.Kind);
            Assert.AreEqual(token, st.Content);
        }

        [TestMethod]
        public void Test_GetFactory()
        {
            Assert.AreEqual(SourceLanguage.CSharp, SourceCodeUtils.GetFactory(".cs").Language);
            Assert.AreEqual(SourceLanguage.VisualBasic, SourceCodeUtils.GetFactory(".vb").Language);
            Assert.AreEqual(SourceLanguage.Cpp, SourceCodeUtils.GetFactory(".cpp").Language);
            Assert.AreEqual(SourceLanguage.CSharp, SourceCodeUtils.GetFactory(".text").Language);
        }
    }
}
