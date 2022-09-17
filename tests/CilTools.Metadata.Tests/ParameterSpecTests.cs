/* CilTools.Metadata tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Reflection;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class ParameterSpecTests
    {
        [TestMethod]
        public void Test_ParameterSpec()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typeof(SampleMethods).FullName);
                MethodInfo m = t.GetMethod("CalcSum");
                ParameterInfo[] pars = m.GetParameters();
                Assert.AreEqual(2, pars.Length);

                Assert.AreEqual(0, pars[0].Position);
                Assert.AreEqual("x", pars[0].Name);
                Assert.AreEqual("System.Double", pars[0].ParameterType.FullName);
                Assert.AreEqual(DBNull.Value, pars[0].DefaultValue);
                Assert.AreEqual(DBNull.Value, pars[0].RawDefaultValue);
                Assert.AreSame(m, pars[0].Member);
                Assert.AreEqual(ParameterAttributes.None, pars[0].Attributes);
                Assert.IsFalse(pars[0].IsIn);
                Assert.IsFalse(pars[0].IsOut);
                Assert.IsFalse(pars[0].IsOptional);
                Assert.IsFalse(pars[0].IsRetval);

                Assert.AreEqual(1, pars[1].Position);
                Assert.AreEqual("y", pars[1].Name);
                Assert.AreEqual("System.Double", pars[1].ParameterType.FullName);
                Assert.AreEqual(DBNull.Value, pars[1].DefaultValue);
                Assert.AreEqual(DBNull.Value, pars[1].RawDefaultValue);
                Assert.AreSame(m, pars[1].Member);
                Assert.AreEqual(ParameterAttributes.None, pars[1].Attributes);
                Assert.IsFalse(pars[1].IsIn);
                Assert.IsFalse(pars[1].IsOut);
                Assert.IsFalse(pars[1].IsOptional);
                Assert.IsFalse(pars[1].IsRetval);
            }
        }

        [TestMethod]
        [WorkItem(54)]
        public void Test_GetCustomAttributes()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typeof(SampleMethods).FullName);
                MethodInfo m = t.GetMethod("ParameterAttributesTest");
                ParameterInfo[] pars = m.GetParameters();                
                object[] attrs = pars[0].GetCustomAttributes(false);

                Assert.AreEqual(1, attrs.Length);
                ICustomAttribute ica = (ICustomAttribute)attrs[0];
                Assert.AreEqual("MyAttribute", ica.Constructor.DeclaringType.Name);
                Assert.AreEqual(typeof(MyAttribute).FullName, ica.Constructor.DeclaringType.FullName);
                byte[] expectedData = new byte[] { 0x01, 0, 0x7b, 0, 0, 0, 0, 0 };
                byte[] data = ica.Data;
                CollectionAssert.AreEqual(expectedData, data);
            }
        }

        [TestMethod]
        [WorkItem(54)]
        public void Test_GetCustomAttributes_Negative()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typeof(SampleMethods).FullName);
                MethodInfo m = t.GetMethod("CalcSum");
                ParameterInfo[] pars = m.GetParameters();
                object[] attrs = pars[0].GetCustomAttributes(false);

                Assert.AreEqual(0, attrs.Length);
            }
        }

        [TestMethod]
        public void Test_HasDefaultValue()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typeof(SampleMethods).FullName);
                MethodInfo m = t.GetMethod("CalcSum");
                ParameterInfo[] pars = m.GetParameters();
                Assert.IsFalse(pars[0].HasDefaultValue);
                Assert.IsFalse(pars[1].HasDefaultValue);
            }
        }

        [TestMethod]
        public void Test_DefaultValue()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typeof(SampleMethods).FullName);
                MethodInfo m = t.GetMethod("TestOptionalParams");
                ParameterInfo[] pars = m.GetParameters();
                Assert.IsTrue(pars[0].HasDefaultValue);
                Assert.AreEqual(string.Empty, pars[0].DefaultValue);
                Assert.AreEqual(string.Empty, pars[0].RawDefaultValue);
                Assert.IsTrue(pars[1].HasDefaultValue);
                Assert.AreEqual(0, pars[1].DefaultValue);
                Assert.AreEqual(0, pars[1].RawDefaultValue);
            }
        }
    }
}
