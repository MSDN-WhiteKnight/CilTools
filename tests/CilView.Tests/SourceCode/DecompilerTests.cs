/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Tests.Common;
using CilTools.Tests.Common.TestData;
using CilView.SourceCode;
using CilView.Core.Syntax;

namespace CilView.Tests.SourceCode
{
    [TestClass]
    public class DecompilerTests
    {
        [TestMethod]
        [DataRow(".cs", "public static void PrintHelloWorld()")]
        [DataRow(".cpp", "public: static void PrintHelloWorld(){")]
        [DataRow(".txt", "public static void PrintHelloWorld()")]
        public void Test_Decompiler_HelloWorld(string lang, string expected)
        {
            MethodBase mb = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            string s = Decompiler.GetMethodSignatureString(lang, mb);
            Assert.AreEqual(expected, s);
        }

        [TestMethod]
        [DataRow(".cs", "public static double CalcSum(double x, double y)")]
        [DataRow(".cpp", "public: static double CalcSum(double x, double y){")]
        public void Test_Decompiler_CalcSum(string lang, string expected)
        {
            MethodBase mb = typeof(SampleMethods).GetMethod("CalcSum");
            string s = Decompiler.GetMethodSignatureString(lang, mb);
            Assert.AreEqual(expected, s);
        }

        [TestMethod]
        [DataRow(".cs", "public static T[] GenerateArray<T>(int len)")]
        [DataRow(".cpp", "public: generic <typename T>\nstatic array <T ^> ^ GenerateArray(int len){")]
        public void Test_Decompiler_Generic(string lang, string expected)
        {
            MethodBase mb = typeof(SampleMethods).GetMethod("GenerateArray");
            string s = Decompiler.GetMethodSignatureString(lang, mb);
            Assert.AreEqual(expected, s.Replace("\r\n","\n"));
        }

        [TestMethod]
        [DataRow(".cs", "public float X {get;}")]
        [DataRow(".cpp", "public: float get_X(){")]
        public void Test_Decompiler_Property(string lang, string expected)
        {
            MethodBase mb = typeof(MyPoint).GetMethod("get_X");
            string s = Decompiler.GetMethodSignatureString(lang, mb);            
            Assert.AreEqual(expected, s);
        }

        [TestMethod]
        [DataRow(".cs", "void Bar(string s, object o);")]
        [DataRow(".cpp", "public: void Bar(String ^ s, Object ^ o){")]
        public void Test_Decompiler_InterfaceMethod(string lang, string expected)
        {
            MethodBase mb = typeof(ITest).GetMethod("Bar");
            string s = Decompiler.GetMethodSignatureString(lang, mb);
            Assert.AreEqual(expected, s);
        }

        [TestMethod]
        [DataRow(".cs", "public void Bar(string s, object o)")]
        [DataRow(".cpp", "public: void Bar(String ^ s, Object ^ o){")]
        public void Test_Decompiler_InstanceMethod(string lang, string expected)
        {
            MethodBase mb = typeof(InterfacesSampleType).GetMethod("Bar");
            string s = Decompiler.GetMethodSignatureString(lang, mb);
            Assert.AreEqual(expected, s);
        }

        [TestMethod]        
        public void Test_Decompiler_ByRef()
        {
            MethodBase mb = typeof(SampleMethods).GetMethod("DivideNumbers");
            string s = Decompiler.GetMethodSignatureString(".cs", mb);
            string expected = "public static bool DivideNumbers(int x, int y, ref int result)";
            Assert.AreEqual(expected, s);
        }

        [TestMethod]
        public void Test_Decompiler_Pointer()
        {
            MethodBase mb = typeof(Pointer).GetMethod("Box");
            string s = Decompiler.GetMethodSignatureString(".cs", mb);
            string expected = "public static object Box(void* ptr, Type type)";
            Assert.AreEqual(expected, s);
        }

        [TestMethod]
        public void Test_Decompiler_GenericType()
        {
            MethodBase mb = typeof(IEnumerable<>).GetMethod("GetEnumerator");
            string s = Decompiler.GetMethodSignatureString(".cs", mb);
            string expected = "IEnumerator<T> GetEnumerator();";
            Assert.AreEqual(expected, s);
        }

        [TestMethod]        
        public void Test_DecompileMethodSignature_Csharp()
        {
            MethodBase mb = typeof(SampleMethods).GetMethod("CalcSum");
            SourceToken[] tokens = Decompiler.DecompileMethodSignature(".cs", mb).ToArray();
            Assert.AreEqual(14, tokens.Length);

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
            Assert.AreEqual("double", tokens[2].Content);
            Assert.AreEqual(string.Empty, tokens[2].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[2].TrailingWhitespace);
            Assert.AreEqual("double", tokens[2].ToString());

            Assert.AreEqual(TokenKind.FunctionName, tokens[4].Kind);
            Assert.AreEqual("CalcSum", tokens[4].Content);
            Assert.AreEqual(string.Empty, tokens[4].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[4].TrailingWhitespace);
            Assert.AreEqual("CalcSum", tokens[4].ToString());

            Assert.AreEqual(TokenKind.Punctuation, tokens[5].Kind);
            Assert.AreEqual("(", tokens[5].Content);
            Assert.AreEqual(string.Empty, tokens[5].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[5].TrailingWhitespace);
            Assert.AreEqual("(", tokens[5].ToString());

            Assert.AreEqual(TokenKind.Keyword, tokens[6].Kind);
            Assert.AreEqual("double", tokens[6].Content);
            Assert.AreEqual(string.Empty, tokens[6].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[6].TrailingWhitespace);
            Assert.AreEqual("double", tokens[6].ToString());

            Assert.AreEqual(TokenKind.Name, tokens[8].Kind);
            Assert.AreEqual("x", tokens[8].Content);
            Assert.AreEqual(string.Empty, tokens[8].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[8].TrailingWhitespace);
            Assert.AreEqual("x", tokens[8].ToString());

            Assert.AreEqual(TokenKind.Punctuation, tokens[9].Kind);
            Assert.AreEqual(",", tokens[9].Content);
            Assert.AreEqual(string.Empty, tokens[9].LeadingWhitespace);
            Assert.AreEqual(" ", tokens[9].TrailingWhitespace);
            Assert.AreEqual(", ", tokens[9].ToString());

            Assert.AreEqual(TokenKind.Keyword, tokens[10].Kind);
            Assert.AreEqual("double", tokens[10].Content);
            Assert.AreEqual(string.Empty, tokens[10].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[10].TrailingWhitespace);
            Assert.AreEqual("double", tokens[10].ToString());

            Assert.AreEqual(TokenKind.Name, tokens[12].Kind);
            Assert.AreEqual("y", tokens[12].Content);
            Assert.AreEqual(string.Empty, tokens[12].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[12].TrailingWhitespace);
            Assert.AreEqual("y", tokens[12].ToString());

            Assert.AreEqual(TokenKind.Punctuation, tokens[13].Kind);
            Assert.AreEqual(")", tokens[13].Content);
            Assert.AreEqual(string.Empty, tokens[13].LeadingWhitespace);
            Assert.AreEqual(string.Empty, tokens[13].TrailingWhitespace);
            Assert.AreEqual(")", tokens[13].ToString());
        }
    }
}
