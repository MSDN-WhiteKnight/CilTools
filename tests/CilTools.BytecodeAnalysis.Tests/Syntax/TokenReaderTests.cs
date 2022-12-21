/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Syntax.Tokens;

namespace CilTools.BytecodeAnalysis.Tests.Syntax
{
    [TestClass]
    public class TokenReaderTests
    {
        [TestMethod]
        public void Test_TokenReader_ReadAll()
        {
            string s = "int i=1*2/0.5; /*число*/ string s1 = \"Hello, world\";/*string2*/ char c='\\'';";
            TokenReader reader = new TokenReader(s, SyntaxTokenDefinition.IlasmTokens);
            string[] tokens = reader.ReadAll().ToArray();

            CollectionAssert.AreEqual(new string[] {
                "int"," ","i","=","1","*","2","/","0.5",";"," ","/*число*/"," ","string"," ","s1"," ","="," ",
                "\"Hello, world\"",";","/*string2*/"," ","char"," ","c","=","'\\''",";"
            }, tokens);
        }

        [TestMethod]
        public void Test_TokenReader_Call()
        {
            string s = "call       void [mscorlib]System.Console::WriteLine(string)";
            TokenReader reader = new TokenReader(s, SyntaxTokenDefinition.IlasmTokens);
            string[] tokens = reader.ReadAll().ToArray();

            CollectionAssert.AreEqual(new string[] {
                "call","       ","void"," ","[","mscorlib","]","System.Console",":",":","WriteLine","(","string",")"
            }, tokens);
        }

        [TestMethod]
        public void Test_TokenReader_DottedName()
        {
            string s = "ldc.i4.1";
            TokenReader reader = new TokenReader(s, SyntaxTokenDefinition.IlasmTokens);
            string[] tokens = reader.ReadAll().ToArray();
            CollectionAssert.AreEqual(new string[] {"ldc.i4.1"}, tokens);
        }

        [TestMethod]
        public void Test_TokenReader_StringLiteral()
        {
            string s = "ldstr \"Hello, World\"";
            TokenReader reader = new TokenReader(s, SyntaxTokenDefinition.IlasmTokens);
            string[] tokens = reader.ReadAll().ToArray();
            CollectionAssert.AreEqual(new string[] { "ldstr"," ", "\"Hello, World\""}, tokens);
        }

        [TestMethod]
        public void Test_TokenReader_Escaping()
        {
            //string s1="\"";string s2="\\";char c1='\'';char c2='\\';Foo();
            string s = @"string s1=""\"""";string s2=""\\"";char c1='\'';char c2='\\';Foo();";
            TokenReader reader = new TokenReader(s, SyntaxTokenDefinition.IlasmTokens);
            string[] tokens = reader.ReadAll().ToArray();

            CollectionAssert.AreEqual(new string[] { 
                "string", " ", "s1", "=" , @"""\""""", ";", "string", " ", "s2", "=", @"""\\""", ";",
                "char", " ", "c1", "=", @"'\''", ";", "char", " ", "c2", "=", @"'\\'", ";", 
                "Foo", "(", ")", ";"
            }, tokens);
        }

        [TestMethod]
        public void Test_TokenReader_NumericLiteral()
        {
            string s = "IL_0001: ldc.i4.s 10";
            TokenReader reader = new TokenReader(s, SyntaxTokenDefinition.IlasmTokens);
            string[] tokens = reader.ReadAll().ToArray();
            CollectionAssert.AreEqual(new string[] { "IL_0001", ":", " ", "ldc.i4.s", " ", "10" }, tokens);
        }

        [TestMethod]
        public void Test_TokenReader_SingleQuotLiteral()
        {
            string s = ".method public hidebysig static void 'method'() cil managed";
            TokenReader reader = new TokenReader(s, SyntaxTokenDefinition.IlasmTokens);
            string[] tokens = reader.ReadAll().ToArray();

            CollectionAssert.AreEqual(new string[] {
                ".method", " ", "public", " ", "hidebysig", " ", "static", " ",
                "void", " ", "'method'", "(", ")", " ", "cil", " ", "managed",
            }, tokens);
        }

        [TestMethod]
        public void Test_TokenReader_Comment()
        {
            string s = "ldc.i4.1 //load integer value onto the stack\r\nadd";
            TokenReader reader = new TokenReader(s, SyntaxTokenDefinition.IlasmTokens);
            string[] tokens = reader.ReadAll().ToArray();

            CollectionAssert.AreEqual(new string[] { 
                "ldc.i4.1"," ", "//load integer value onto the stack", "\r\n", "add"
            }, tokens);
        }

        public const string Data_MultilineString =
            @".method public hidebysig static float64 CalcSum(float64 x, float64 y) cil managed {
 .maxstack 2
 .locals init (float64 V_0)

          nop
          ldarg.0
          ldarg.1
          add
          stloc.0
          br.s         IL_0001
 IL_0001: ldloc.0
          ret
}";

        [TestMethod]
        public void Test_TokenReader_Lines()
        {
            string s = Data_MultilineString.Replace("\r\n", "\n");
            TokenReader reader = new TokenReader(s, SyntaxTokenDefinition.IlasmTokens);
            string[] tokens = reader.ReadAll().ToArray();

            string[] expected = new string[] {
                ".method"," ","public"," ","hidebysig"," ","static"," ","float64"," ", "CalcSum", "(",
                "float64"," ","x",","," ","float64"," ","y",")"," ","cil", " ", "managed"," ","{","\n ",
                ".maxstack"," ","2","\n ",
                ".locals"," ","init"," ","(","float64"," ","V_0",")","\n\n          ",
                "nop","\n          ",
                "ldarg.0","\n          ",
                "ldarg.1","\n          ",
                "add","\n          ",
                "stloc.0","\n          ",
                "br.s","         ","IL_0001","\n ",
                "IL_0001",":"," ", "ldloc.0","\n          ",
                "ret","\n","}"
            };

            CollectionAssert.AreEqual(expected, tokens);
        }

        [TestMethod]
        public void Test_TokenReader_GenericArity()
        {
            string s = "ValueTuple`1<T>";
            TokenReader reader = new TokenReader(s, SyntaxTokenDefinition.IlasmTokens);
            string[] tokens = reader.ReadAll().ToArray();
            CollectionAssert.AreEqual(new string[] { "ValueTuple`1", "<", "T", ">" }, tokens);
        }

        [TestMethod]
        public void Test_TokenReader_IdSpecialChars()
        {
            string s = ".class public @MyClass$ {";
            TokenReader reader = new TokenReader(s, SyntaxTokenDefinition.IlasmTokens);
            string[] tokens = reader.ReadAll().ToArray();

            CollectionAssert.AreEqual(
                new string[] { ".class", " ", "public", " ", "@MyClass$", " ", "{"}, 
                tokens);
        }
    }
}
