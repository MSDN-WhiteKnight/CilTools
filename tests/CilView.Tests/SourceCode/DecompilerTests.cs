/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Tests.Common;
using CilTools.Tests.Common.TestData;
using CilView.SourceCode;

namespace CilView.Tests.SourceCode
{
    [TestClass]
    public class DecompilerTests
    {
        [TestMethod]
        [DataRow(".cs", "public static void PrintHelloWorld()")]
        [DataRow(".cpp", "public: static void PrintHelloWorld() {")]
        [DataRow(".txt", "public static void PrintHelloWorld()")]
        public void Test_Decompiler_HelloWorld(string lang, string expected)
        {
            MethodBase mb = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            string s = Decompiler.DecompileMethodSignature(lang, mb);
            Assert.AreEqual(expected, s);
        }

        [TestMethod]
        [DataRow(".cs", "public static double CalcSum(double x, double y)")]
        [DataRow(".cpp", "public: static double CalcSum(double x, double y) {")]
        public void Test_Decompiler_CalcSum(string lang, string expected)
        {
            MethodBase mb = typeof(SampleMethods).GetMethod("CalcSum");
            string s = Decompiler.DecompileMethodSignature(lang, mb);
            Assert.AreEqual(expected, s);
        }

        [TestMethod]
        [DataRow(".cs", "public static T[] GenerateArray<T>(int len)")]
        [DataRow(".cpp", "public: generic <typename T>\nstatic array <T ^> ^ GenerateArray(int len) {")]
        public void Test_Decompiler_Generic(string lang, string expected)
        {
            MethodBase mb = typeof(SampleMethods).GetMethod("GenerateArray");
            string s = Decompiler.DecompileMethodSignature(lang, mb);
            Assert.AreEqual(expected, s.Replace("\r\n","\n"));
        }

        [TestMethod]
        [DataRow(".cs", "public float X {get;}")]
        [DataRow(".cpp", "public: float get_X() {")]
        public void Test_Decompiler_Property(string lang, string expected)
        {
            MethodBase mb = typeof(MyPoint).GetMethod("get_X");
            string s = Decompiler.DecompileMethodSignature(lang, mb);            
            Assert.AreEqual(expected, s);
        }

        [TestMethod]
        [DataRow(".cs", "void Bar(string s, object o);")]
        [DataRow(".cpp", "public: void Bar(String ^ s, Object ^ o) {")]
        public void Test_Decompiler_InterfaceMethod(string lang, string expected)
        {
            MethodBase mb = typeof(ITest).GetMethod("Bar");
            string s = Decompiler.DecompileMethodSignature(lang, mb);
            Assert.AreEqual(expected, s);
        }

        [TestMethod]
        [DataRow(".cs", "public void Bar(string s, object o)")]
        [DataRow(".cpp", "public: void Bar(String ^ s, Object ^ o) {")]
        public void Test_Decompiler_InstanceMethod(string lang, string expected)
        {
            MethodBase mb = typeof(InterfacesSampleType).GetMethod("Bar");
            string s = Decompiler.DecompileMethodSignature(lang, mb);
            Assert.AreEqual(expected, s);
        }
    }
}
