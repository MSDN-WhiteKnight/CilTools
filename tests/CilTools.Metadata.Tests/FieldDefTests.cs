/* CilTools.Metadata tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Reflection;
using CilTools.Tests.Common;
using CilTools.Tests.Common.Attributes;
using CilTools.Tests.Common.TestData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class FieldDefTests
    {
        [TestMethod]
        [TypeTestData(typeof(FieldsSampleType), BytecodeProviders.Metadata)]
        public void Test_FieldDef(Type t)
        {
            FieldInfo fi = t.GetField("x");
            Assert.AreEqual("x", fi.Name);
            Assert.AreEqual("System.String", fi.FieldType.FullName);
            Assert.AreSame(t, fi.DeclaringType);
            Assert.IsTrue(fi.IsPublic);
            Assert.IsFalse(fi.IsStatic);
            Assert.AreEqual(MemberTypes.Field, fi.MemberType);

            fi = t.GetField("y");
            Assert.AreEqual("y", fi.Name);
            Assert.AreEqual("System.Int32", fi.FieldType.FullName);
            Assert.AreSame(t, fi.DeclaringType);
            Assert.IsTrue(fi.IsPublic);
            Assert.IsTrue(fi.IsStatic);
            Assert.AreEqual(MemberTypes.Field, fi.MemberType);
        }

        [TestMethod]
        [TypeTestData(typeof(FieldsSampleType), BytecodeProviders.Metadata)]
        public void Test_GetCustomAttributes(Type t)
        {
            FieldInfo fi = t.GetField("x");
            object[] attrs = fi.GetCustomAttributes(false);

            Assert.AreEqual(1, attrs.Length);
            ICustomAttribute ica = (ICustomAttribute)attrs[0];
            Assert.AreEqual("MyAttribute", ica.Constructor.DeclaringType.Name);
        }

        [TestMethod]
        [TypeTestData(typeof(FieldsSampleType), BytecodeProviders.Metadata)]
        public void Test_GetCustomAttributes_Negative(Type t)
        {
            FieldInfo fi = t.GetField("y");
            object[] attrs = fi.GetCustomAttributes(false);

            Assert.AreEqual(0, attrs.Length);
        }

        [TestMethod]
        [TypeTestData(typeof(ExplicitStructSample), BytecodeProviders.Metadata)]
        public void Test_FieldOffset(Type t)
        {
            FieldInfo fi = t.GetField("x");
            int offset = (int)ReflectionProperties.Get(fi, ReflectionProperties.FieldOffset);
            Assert.AreEqual(1, offset);
        }

        [TestMethod]
        [TypeTestData(typeof(FieldsSampleType), BytecodeProviders.Metadata)]
        public void Test_FieldOffset_Negative(Type t)
        {
            FieldInfo fi = t.GetField("y");
            int offset = (int)ReflectionProperties.Get(fi, ReflectionProperties.FieldOffset);
            Assert.AreEqual(-1, offset);
        }
    }
}
