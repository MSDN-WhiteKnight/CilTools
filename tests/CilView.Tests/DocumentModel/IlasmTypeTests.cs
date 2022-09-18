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
    public class IlasmTypeTests
    {
        static SyntaxNode[] GetTestTypeTokens(string name)
        {
            SyntaxNode[] tokens = new SyntaxNode[] {
                SyntaxFactory.CreateFromToken("/* Test type */", string.Empty, " "),
                new DocumentSyntax(new SyntaxNode[]{
                    SyntaxFactory.CreateFromToken(".class", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("private", string.Empty, " "),
                    SyntaxFactory.CreateFromToken(name, string.Empty, " "),
                    SyntaxFactory.CreateFromToken("{", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("}", string.Empty, " ")
                }, ".class", false, string.Empty)
            };

            return tokens;
        }

        [TestMethod]
        public void Test_IlasmType()
        {
            IlasmAssembly ass = new IlasmAssembly(new DocumentSyntax("TestAssembly"), "TestAssembly", string.Empty);
            const string typeName = "MyNamespace.TestClass";
            DocumentSyntax ds = new DocumentSyntax(GetTestTypeTokens(typeName), typeName, false, string.Empty);
            IlasmType t = new IlasmType(ass, ds, typeName);

            string il = "/* Test type */ .class private MyNamespace.TestClass { } ";
            Assert.AreEqual(il, t.GetDocumentText());
            Assert.AreEqual(typeName, t.FullName);
            Assert.AreEqual("TestClass", t.Name);
            Assert.AreEqual("MyNamespace", t.Namespace);
            Assert.AreEqual(typeName, t.ToString()); //used by WPF, should not throw!
        }

        [TestMethod]
        public void Test_IlasmType_Equality()
        {
            //equality is used by WPF so we need to ensure it does not throw

            IlasmAssembly ass = new IlasmAssembly(new DocumentSyntax("TestAssembly"), "TestAssembly", string.Empty);
            const string typeName1 = "MyNamespace.TestClass";
            DocumentSyntax ds1 = new DocumentSyntax(GetTestTypeTokens(typeName1), typeName1, false, string.Empty);
            IlasmType t1 = new IlasmType(ass, ds1, typeName1);
            const string typeName2 = "MyNamespace.TestClass2";
            DocumentSyntax ds2 = new DocumentSyntax(GetTestTypeTokens(typeName2), typeName2, false, string.Empty);
            IlasmType t2 = new IlasmType(ass, ds2, typeName2);
            
            Assert.AreEqual(t1, t1);
            Assert.AreEqual(t2, t2);
            Assert.AreNotEqual(t1, t2);
            Assert.AreNotEqual(t2, t1);
            Assert.AreNotEqual(t1, typeof(object));
            Assert.AreNotEqual(t2, typeof(object));

            AssertThat.DoesNotThrow(() => { int _ = t1.GetHashCode(); });
            AssertThat.DoesNotThrow(() => { int _ = t2.GetHashCode(); });
        }
    }
}
