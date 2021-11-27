/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
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
    public class TypeWithProperties
    {
        public string Name { get; set; }
        public int Number { get; }
    }

    [TestClass]
    public class PropertyDefTests
    {
        const string SampleTypeName = "CilTools.Metadata.Tests.TypeWithProperties";

        [TestMethod]
        public void Test_PropertyDef_String()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(TypeWithProperties).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                PropertyInfo pi=t.GetProperty("Name");
                Assert.AreEqual(MemberTypes.Property, pi.MemberType);
                Assert.AreEqual(PropertyAttributes.None, pi.Attributes);
                Assert.IsTrue(pi.CanRead);
                Assert.IsTrue(pi.CanWrite);
                Assert.AreEqual("Name", pi.Name);
                Assert.AreEqual("String", pi.PropertyType.Name);
                Assert.AreSame(t, pi.DeclaringType);
                Assert.AreEqual(SampleTypeName, pi.DeclaringType.FullName);
                Assert.AreEqual(0, pi.GetCustomAttributes(false).Length);
                AssertThat.Throws<InvalidOperationException>(() => pi.GetValue(null));
                AssertThat.Throws<InvalidOperationException>(() => pi.SetValue(null,null));
            }
        }

        [TestMethod]
        public void Test_PropertyDef_Integer()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(TypeWithProperties).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                PropertyInfo pi = t.GetProperty("Number");                
                Assert.IsTrue(pi.CanRead);
                Assert.IsFalse(pi.CanWrite);
                Assert.AreEqual("Number", pi.Name);
                Assert.AreEqual("Int32", pi.PropertyType.Name);
            }
        }
    }
}
