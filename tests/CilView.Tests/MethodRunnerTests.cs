/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Metadata;
using CilTools.Tests.Common;
using CilView.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilView.Tests
{
    [TestClass]
    public class MethodRunnerTests
    {
        [TestMethod]
        public void Test_GetRuntimeMethod()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typeof(SampleMethods).FullName);
                MethodBase miMetadata = t.GetMethod("PrintHelloWorld");
                MethodBase miRuntime = MethodRunner.GetRuntimeMethod(miMetadata);
                Assert.AreEqual(miMetadata.Name, miRuntime.Name);
                Assert.AreEqual("PrintHelloWorld", miRuntime.Name);
                AssertThat.DoesNotThrow(() => miRuntime.Invoke(null, new object[0]));
            }
        }

        public static void Print(string s)
        {
            Console.WriteLine(s);
        }

        public static void Print(int x, int y)
        {
            Console.WriteLine((x + y).ToString());
        }

        [TestMethod]
        public void Test_GetRuntimeMethod_Overloads()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(MethodRunnerTests).Assembly.Location);
                Type t = ass.GetType(typeof(MethodRunnerTests).FullName);
                MethodBase miMetadata = t.GetMethod("Print", new Type[] { typeof(string) });
                MethodBase miRuntime = MethodRunner.GetRuntimeMethod(miMetadata);
                Assert.AreEqual(miMetadata.Name, miRuntime.Name);
                Assert.AreEqual("Print", miRuntime.Name);
                Assert.AreEqual(1, miRuntime.GetParameters().Length);

                miMetadata = t.GetMethod("Print", new Type[] { typeof(int) , typeof(int) });
                miRuntime = MethodRunner.GetRuntimeMethod(miMetadata);
                Assert.AreEqual(miMetadata.Name, miRuntime.Name);
                Assert.AreEqual("Print", miRuntime.Name);
                Assert.AreEqual(2, miRuntime.GetParameters().Length);
            }
        }

        [TestMethod]
        public void Test_GetRuntimeMethod_ReturnValue()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typeof(SampleMethods).FullName);
                MethodBase miMetadata = t.GetMethod("CalcSum");
                MethodBase miRuntime = MethodRunner.GetRuntimeMethod(miMetadata);
                Assert.AreEqual(miMetadata.Name, miRuntime.Name);
                Assert.AreEqual("CalcSum", miRuntime.Name);
                object res = miRuntime.Invoke(null, new object[] { 1.1, 2.3 });
                Assert.AreEqual(3.4, (double)res, 0.01);
            }
        }
    }
}
