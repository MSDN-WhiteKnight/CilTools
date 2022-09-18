/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Syntax;
using CilTools.Tests.Common;
using CilView.Core.DocumentModel;
using CilView.Core.Syntax;

namespace CilView.Tests.DocumentModel
{
    [TestClass]
    public class IlasmAssemblyTests
    {
        static SyntaxNode[] GetTestAssemblyTokens()
        {
            SyntaxNode[] tokens = new SyntaxNode[] {
                SyntaxFactory.CreateFromToken("/* Test assembly */", string.Empty, " "),
                new DocumentSyntax(new SyntaxNode[]{
                    SyntaxFactory.CreateFromToken(".assembly", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("TestAssembly", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("{", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("}", string.Empty, " ")
                }, ".assembly", false, string.Empty)
            };

            return tokens;
        }

        static SyntaxNode[] GetTestTypeTokens(string name)
        {
            SyntaxNode[] tokens = new SyntaxNode[] {
                SyntaxFactory.CreateFromToken("/* Test type */", string.Empty, " "),
                new DocumentSyntax(new SyntaxNode[]{
                    SyntaxFactory.CreateFromToken(".class", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("public", string.Empty, " "),
                    SyntaxFactory.CreateFromToken(name, string.Empty, " "),
                    SyntaxFactory.CreateFromToken("{", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("}", string.Empty, " ")
                }, ".class", false, string.Empty)
            };

            return tokens;
        }

        [TestMethod]
        public void Test_IlasmAssembly()
        {
            SyntaxNode[] tokens = GetTestAssemblyTokens();
            DocumentSyntax ds = new DocumentSyntax(tokens);
            IlasmAssembly ass = new IlasmAssembly(ds, "TestAssembly", ds.ToString());
            Assert.AreEqual("TestAssembly", ass.GetName().Name);
            Assert.AreEqual("TestAssembly", ass.FullName);
            Assert.AreEqual("TestAssembly", ass.ToString()); //used by WPF, should not throw!
            Assert.IsTrue(ass.ReflectionOnly);
            Assert.IsFalse(ass.IsDynamic);
            Assert.AreEqual(string.Empty, ass.Location);
            Assert.AreEqual(string.Empty, ass.CodeBase);
            Assert.AreSame(ds, ass.Syntax);
            Assert.AreEqual(0, ass.GetTypes().Length);

            const string il = "/* Test assembly */ .assembly TestAssembly { } ";
            Assert.AreEqual(il, ass.GetDocumentText());
        }

        [TestMethod]
        public void Test_IlasmAssembly_AddType()
        {
            SyntaxNode[] tokens = GetTestAssemblyTokens();
            DocumentSyntax ds = new DocumentSyntax(tokens);
            IlasmAssembly ass = new IlasmAssembly(ds, "TestAssembly", ds.ToString());
            ass.AddType(new IlasmType(ass, new DocumentSyntax(GetTestTypeTokens("Foo")), "Foo"));
            ass.AddType(new IlasmType(ass, new DocumentSyntax(GetTestTypeTokens("Bar")), "Bar"));

            Type[] types = ass.GetTypes();
            Assert.AreEqual(2, types.Length);
            Assert.AreEqual("Foo", types[0].Name);
            Assert.AreEqual("Bar", types[1].Name);
        }
    }
}
