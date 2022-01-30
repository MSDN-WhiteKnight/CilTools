/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using CilTools.Metadata;
using CilTools.Tests.Common;
using CilView.Core;
using CilView.Core.Reflection;
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

        [TestMethod]
        public void Test_ExecuteMethod()
        {
            MethodBase method = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            MethodParameter[] pars = new MethodParameter[0];
            
            MethodExecutionResults res = MethodRunner.ExecuteMethod(method, pars, TimeSpan.FromSeconds(2));

            Assert.IsFalse(res.IsTimedOut);
            Assert.IsNull(res.ReturnValue);
            Assert.AreEqual("System.Void", res.ReturnValueType.FullName);
            Assert.IsNull(res.ExceptionObject);
        }

        [TestMethod]
        public void Test_ExecuteMethod_ReturnValue()
        {
            MethodBase method = typeof(SampleMethods).GetMethod("CalcSum");
            MethodParameter[] pars = MethodRunner.GetMethodParameters(method);
            pars[0].Value = "1";
            pars[1].Value = "2";

            MethodExecutionResults res = MethodRunner.ExecuteMethod(method, pars, TimeSpan.FromSeconds(2));

            Assert.IsFalse(res.IsTimedOut);
            Assert.AreEqual(3.0, (double)res.ReturnValue);
            Assert.AreEqual("System.Double", res.ReturnValueType.FullName);
            Assert.IsNull(res.ExceptionObject);
        }

        [TestMethod]
        public void Test_ExecuteMethod_ByRef()
        {
            MethodBase method = typeof(SampleMethods).GetMethod("DivideNumbers");
            MethodParameter[] pars = MethodRunner.GetMethodParameters(method);
            pars[0].Value = "4";
            pars[1].Value = "2";
            pars[2].Value = "0";

            MethodExecutionResults res = MethodRunner.ExecuteMethod(method, pars, TimeSpan.FromSeconds(2));

            Assert.IsFalse(res.IsTimedOut);
            Assert.AreEqual(true, (bool)res.ReturnValue);
            Assert.IsNull(res.ExceptionObject);
            Assert.AreEqual(2, (int)res.ParameterValues[2]);
        }

        [TestMethod]
        public void Test_ExecuteMethod_Timeout()
        {
            MethodBase method = typeof(Thread).GetMethod("Sleep", new Type[] { typeof(int) });
            MethodParameter[] pars = MethodRunner.GetMethodParameters(method);
            pars[0].Value = "2000";

            MethodExecutionResults res = MethodRunner.ExecuteMethod(method, pars, TimeSpan.FromSeconds(1));

            Assert.IsTrue(res.IsTimedOut);
            Assert.IsNull(res.ReturnValue);
            Assert.IsNull(res.ExceptionObject);
        }
    }
}
