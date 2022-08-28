/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Syntax;
using CilTools.Tests.Common;
using CilView.Core.Syntax;

namespace CilView.Tests
{
    [TestClass]
    public class IlAsmParserTests
    {
        [TestMethod]
        public void Test_TokensToInitialTree_Simple()
        {
            SyntaxNode[] tokens = new SyntaxNode[] {
                SyntaxFactory.CreateFromToken(".class", string.Empty, " "),
                SyntaxFactory.CreateFromToken("private", string.Empty, " "),
                SyntaxFactory.CreateFromToken("Foo", string.Empty, " "),
                SyntaxFactory.CreateFromToken("{", string.Empty, " "),
                SyntaxFactory.CreateFromToken(".field", string.Empty, " "),
                SyntaxFactory.CreateFromToken("private", string.Empty, " "),
                SyntaxFactory.CreateFromToken("int32", string.Empty, " "),
                SyntaxFactory.CreateFromToken("Bar", string.Empty, " "),
                SyntaxFactory.CreateFromToken("}", string.Empty, " "),
            };

            SyntaxNode tree = IlasmParser.TokensToInitialTree(tokens);

            SyntaxNode[] items = tree.GetChildNodes();
            Assert.AreEqual(4, items.Length);
            Assert.AreEqual(".class ", items[0].ToString());
            Assert.AreEqual("private ", items[1].ToString());
            Assert.AreEqual("Foo ", items[2].ToString());
            Assert.AreEqual("{ .field private int32 Bar } ", items[3].ToString());            
        }
    }
}
