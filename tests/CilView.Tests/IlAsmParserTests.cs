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

            DocumentSyntax tree = IlasmParser.TokensToInitialTree(tokens);
            Assert.AreEqual("(All text)", tree.Name);

            SyntaxNode[] items = tree.GetChildNodes();
            Assert.AreEqual(4, items.Length);
            Assert.AreEqual(".class ", items[0].ToString());
            Assert.AreEqual("private ", items[1].ToString());
            Assert.AreEqual("Foo ", items[2].ToString());
            Assert.AreEqual("{ .field private int32 Bar } ", items[3].ToString());
        }

        [TestMethod]
        public void Test_TokensToInitialTree()
        {
            SyntaxNode[] tokens = new SyntaxNode[] {
                SyntaxFactory.CreateFromToken(".class", string.Empty, " "),
                SyntaxFactory.CreateFromToken("public", string.Empty, " "),
                SyntaxFactory.CreateFromToken("C", string.Empty, " "),
                SyntaxFactory.CreateFromToken("{", string.Empty, "\n"),
                SyntaxFactory.CreateFromToken(".method", string.Empty, " "),
                SyntaxFactory.CreateFromToken("public", string.Empty, " "),
                SyntaxFactory.CreateFromToken("void", string.Empty, " "),
                SyntaxFactory.CreateFromToken("Foo", string.Empty, " "),
                SyntaxFactory.CreateFromToken("(", string.Empty, " "),
                SyntaxFactory.CreateFromToken(")", string.Empty, " "),
                SyntaxFactory.CreateFromToken("cil", string.Empty, " "),
                SyntaxFactory.CreateFromToken("managed", string.Empty, " "),
                SyntaxFactory.CreateFromToken("{", string.Empty, " "),
                SyntaxFactory.CreateFromToken("}", string.Empty, "\n"),
                SyntaxFactory.CreateFromToken(".method", string.Empty, " "),
                SyntaxFactory.CreateFromToken("public", string.Empty, " "),
                SyntaxFactory.CreateFromToken("void", string.Empty, " "),
                SyntaxFactory.CreateFromToken("Bar", string.Empty, " "),
                SyntaxFactory.CreateFromToken("(", string.Empty, " "),
                SyntaxFactory.CreateFromToken(")", string.Empty, " "),
                SyntaxFactory.CreateFromToken("cil", string.Empty, " "),
                SyntaxFactory.CreateFromToken("managed", string.Empty, " "),
                SyntaxFactory.CreateFromToken("{", string.Empty, " "),
                SyntaxFactory.CreateFromToken("}", string.Empty, "\n"),
                SyntaxFactory.CreateFromToken("}", string.Empty, string.Empty)
            };

            DocumentSyntax tree = IlasmParser.TokensToInitialTree(tokens);
            Assert.AreEqual("(All text)", tree.Name);

            SyntaxNode[] items = tree.GetChildNodes();
            
            Assert.AreEqual(4, items.Length);
            Assert.AreEqual(".class ", items[0].ToString());
            Assert.AreEqual("public ", items[1].ToString());
            Assert.AreEqual("C ", items[2].ToString());

            const string expected = "{\n"+
                ".method public void Foo ( ) cil managed { }\n" +
                ".method public void Bar ( ) cil managed { }\n" + 
                "}";

            Assert.AreEqual(expected, items[3].ToString());

            SyntaxNode[] blockItems = items[3].GetChildNodes();
            Assert.AreEqual(20, blockItems.Length);
            Assert.AreEqual("{ }\n", blockItems[9].ToString());
            Assert.AreEqual("{ }\n", blockItems[18].ToString());
        }

        [TestMethod]
        [DataRow(".class public Foo { } .class public Bar { }")]
        [DataRow(".class public A { .method static void B (int32 x) cil managed { nop } }")]
        [DataRow(TokenReaderTests.Data_MultilineString, DisplayName = "Test_TokensToInitialTree_Roundtrip(Data_MultilineString)")]
        public void Test_TokensToInitialTree_Roundtrip(string il)
        {
            SyntaxNode[] tokens = SyntaxReader.ReadAllNodes(il);
            SyntaxNode tree = IlasmParser.TokensToInitialTree(tokens);
            Assert.AreEqual(il, tree.ToString());
        }

        [TestMethod]
        public void Test_TokensToInitialTree_Invalid()
        {
            SyntaxNode[] tokens = new SyntaxNode[] {
                SyntaxFactory.CreateFromToken(".class", string.Empty, " "),
                SyntaxFactory.CreateFromToken("private", string.Empty, " "),
                SyntaxFactory.CreateFromToken("Foo", string.Empty, " "),
                SyntaxFactory.CreateFromToken("}", string.Empty, " ")
            };

            DocumentSyntax tree = IlasmParser.TokensToInitialTree(tokens);
            Assert.IsTrue(tree.IsInvalid);
            Assert.AreEqual("Unexpected closing brace", tree.ParserDiagnostics);
        }

        [TestMethod]
        public void Test_ParseTopLevelDirectives()
        {
            SyntaxNode[] tokens = new SyntaxNode[] {
                SyntaxFactory.CreateFromToken("/* Foo and Bar */", string.Empty, " "),
                SyntaxFactory.CreateFromToken(".class", string.Empty, " "),
                SyntaxFactory.CreateFromToken("private", string.Empty, " "),
                SyntaxFactory.CreateFromToken("Foo", string.Empty, " "),
                new DocumentSyntax(new SyntaxNode[]{
                    SyntaxFactory.CreateFromToken("{", string.Empty, " "),
                    SyntaxFactory.CreateFromToken(".field", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("private", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("int32", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("X", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("}", string.Empty, " ")
                }, string.Empty, false, string.Empty),                
                SyntaxFactory.CreateFromToken(".class", string.Empty, " "),
                SyntaxFactory.CreateFromToken("private", string.Empty, " "),
                SyntaxFactory.CreateFromToken("Bar", string.Empty, " "),
                new DocumentSyntax(new SyntaxNode[]{
                    SyntaxFactory.CreateFromToken("{", string.Empty, " "),
                    SyntaxFactory.CreateFromToken(".field", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("private", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("string", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("Y", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("}", string.Empty, " ")
                }, string.Empty, false, string.Empty),
            };

            DocumentSyntax initialTree = new DocumentSyntax(tokens, "(All text)", false, string.Empty);
            DocumentSyntax tree = IlasmParser.ParseTopLevelDirectives(initialTree);
            SyntaxNode[] items = tree.GetChildNodes();

            Assert.AreEqual(3, items.Length);
            Assert.AreEqual("/* Foo and Bar */ ", items[0].ToString());
            Assert.AreEqual(".class private Foo { .field private int32 X } ", items[1].ToString());
            Assert.AreEqual(".class private Bar { .field private string Y } ", items[2].ToString());
            Assert.AreEqual("(All text)", tree.Name);
        }

        [TestMethod]
        [DataRow(".class public Foo { } .class public Bar { }")]
        [DataRow(".class public A { .method static void B (int32 x) cil managed { nop } }")]
        [DataRow(TokenReaderTests.Data_MultilineString, DisplayName = "Test_TokensToInitialTree_Roundtrip(Data_MultilineString)")]
        public void Test_IlAsmParser_Roundtrip(string il)
        {
            SyntaxNode[] tokens = SyntaxReader.ReadAllNodes(il);
            DocumentSyntax tree = IlasmParser.TokensToInitialTree(tokens);
            tree = IlasmParser.ParseTopLevelDirectives(tree);
            IlasmAssembly ass = IlasmParser.TreeToAssembly(tree);
            Assert.AreEqual(il, ass.GetDocumentText());
        }

        [TestMethod]        
        public void Test_IlAsmParser_FromDisassembler()
        {
            Type t = typeof(SampleMethods);
            IEnumerable<SyntaxNode> tdef = SyntaxNode.GetTypeDefSyntax(t, true, new DisassemblerParams());
            string il = Utils.SyntaxToString(tdef);

            SyntaxNode[] tokens = SyntaxReader.ReadAllNodes(il);
            DocumentSyntax tree = IlasmParser.TokensToInitialTree(tokens);
            tree = IlasmParser.ParseTopLevelDirectives(tree);
            IlasmAssembly ass = IlasmParser.TreeToAssembly(tree);

            Assert.AreEqual(1, ass.Syntax.GetChildNodes().Length);
            Assert.AreEqual(il, ass.GetDocumentText());
            Type[] types = ass.GetTypes();
            Assert.AreEqual(1, types.Length);
            Assert.AreEqual(typeof(SampleMethods).FullName, types[0].FullName);
            Assert.AreEqual("SampleMethods", types[0].Name);
        }

        [TestMethod]
        public void Test_TreeToAssembly()
        {
            SyntaxNode[] tokens = new SyntaxNode[] {
                SyntaxFactory.CreateFromToken("/* Foo and Bar */", string.Empty, " "),
                new DocumentSyntax(new SyntaxNode[]{                    
                    SyntaxFactory.CreateFromToken(".class", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("private", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("Foo", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("{", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("}", string.Empty, " ")
                }, ".class", false, string.Empty),
                new DocumentSyntax(new SyntaxNode[]{
                    SyntaxFactory.CreateFromToken(".class", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("private", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("Bar", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("{", string.Empty, " "),
                    SyntaxFactory.CreateFromToken("}", string.Empty, " ")
                }, ".class", false, string.Empty)
            };

            DocumentSyntax tree = new DocumentSyntax(tokens, "(All text)", false, string.Empty);
            IlasmAssembly ass = IlasmParser.TreeToAssembly(tree);

            Assert.AreEqual("IlasmAssembly", ass.GetName().Name);
            Assert.AreEqual(tree.ToString(), ass.GetDocumentText());
            Assert.IsTrue(ass.ReflectionOnly);
            Assert.IsFalse(ass.IsDynamic);
            Assert.AreEqual(string.Empty, ass.Location);
            Assert.AreEqual(string.Empty, ass.CodeBase);
            Assert.AreEqual("IlasmAssembly", ass.FullName);
            Assert.AreEqual("IlasmAssembly", ass.ToString());

            Type[] types = ass.GetTypes();
            Assert.AreEqual(2, types.Length);
            Assert.AreEqual(".class private Foo { } ", ((IlasmType)types[0]).GetDocumentText());
            Assert.AreEqual(".class private Bar { } ", ((IlasmType)types[1]).GetDocumentText());
            Assert.AreEqual("Foo", types[0].FullName);
            Assert.AreEqual("Bar", types[1].FullName);
            Assert.AreEqual("Foo", types[0].Name);
            Assert.AreEqual("Bar", types[1].Name);
            Assert.AreEqual("Foo", types[0].ToString());
            Assert.AreEqual("Bar", types[1].ToString());

            //equality is used by WPF so we need to ensure it does not throw
            Assert.AreEqual(types[0], types[0]);
            Assert.AreEqual(types[1], types[1]);
            Assert.AreNotEqual(types[0], types[1]);
            Assert.AreNotEqual(types[0], typeof(object));
            Assert.AreNotEqual(types[1], typeof(object));
            AssertThat.DoesNotThrow(() => { int _ = types[0].GetHashCode(); });
            AssertThat.DoesNotThrow(() => { int _ = types[1].GetHashCode(); });
        }
    }
}
