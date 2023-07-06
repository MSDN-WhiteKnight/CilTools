/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Metadata.Tests.TestData;
using CilTools.Reflection;
using CilTools.Syntax;
using CilTools.Tests.Common;
using CilTools.Tests.Common.TextUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    public class TypeWithProperties
    {
        public string Name { get; set; }
        public int Number { get; }
        [My(0)] public SampleType CustomClass { get; set; }
        public string this[int i] { get { return i.ToString(); } }
        private byte[] PrivateArrayProperty { get; set; }

        public byte[] Foo()
        {
            return this.PrivateArrayProperty;
        }
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

        [TestMethod]
        public void Test_PropertyDef_CustomAttributes()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(TypeWithProperties).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                PropertyInfo pi = t.GetProperty("CustomClass");                
                Assert.AreEqual("CustomClass", pi.Name);
                Assert.AreEqual("SampleType", pi.PropertyType.Name);
                object[] attrs = pi.GetCustomAttributes(false);
                Assert.AreEqual(1, attrs.Length);
                Assert.IsTrue(attrs[0] is ICustomAttribute);
                ICustomAttribute attr = (ICustomAttribute)attrs[0];
                Assert.AreEqual("MyAttribute", attr.Constructor.DeclaringType.Name);
            }
        }

        [TestMethod]
        public void Test_PropertyDef_ObjectDisposedException()
        {
            AssemblyReader reader = new AssemblyReader();
            PropertyInfo pi;

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(TypeWithProperties).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                pi = t.GetProperty("Name");
            }

            AssertThat.Throws<ObjectDisposedException>(() => { var x = pi.Name; });
            AssertThat.Throws<ObjectDisposedException>(() => { var x = pi.CanRead; });
            AssertThat.Throws<ObjectDisposedException>(() => { var x = pi.CanWrite; });
            AssertThat.Throws<ObjectDisposedException>(() => { var x = pi.PropertyType; });
        }

        [TestMethod]
        public void Test_PropertyDef_Indexed()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(TypeWithProperties).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                PropertyInfo pi = t.GetProperty("Item");
                Assert.IsTrue(pi.CanRead);
                Assert.IsFalse(pi.CanWrite);
                Assert.AreEqual("String", pi.PropertyType.Name);
                ParameterInfo[] pars = pi.GetIndexParameters();
                Assert.AreEqual(1, pars.Length);
                Assert.AreEqual("Int32",pars[0].ParameterType.Name);

                IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(typeof(TypeWithProperties));
                string s = Utils.SyntaxToString(nodes);

                AssertThat.IsMatch(s, new Text[] {
                    ".class", Text.Any,"public", Text.Any,"TypeWithProperties", Text.Any,"{", Text.Any,
                    ".property", Text.Any,"instance", Text.Any,"string", Text.Any,"Item",Text.Any,
                    "(", Text.Any,"int32", Text.Any,")", Text.Any,
                    "{", Text.Any,
                    ".get", Text.Any,"instance", Text.Any,"string", Text.Any,"get_Item",
                    "(", Text.Any,"int32", Text.Any,")", Text.Any,
                    "}", Text.Any,
                    "}", Text.Any
                });
            }
        }

        [TestMethod]
        public void Test_PropertyDef_GetGetMethod()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(TypeWithProperties).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                PropertyInfo pi = t.GetProperty("Name");
                MethodInfo mi = pi.GetGetMethod();
                Assert.AreEqual("get_Name", mi.Name);
                Assert.AreEqual("System.String", mi.ReturnType.FullName);
                Assert.AreEqual(0, mi.GetParameters().Length);
                Assert.IsTrue(mi.IsPublic);

                pi = t.GetProperty("Number");
                mi = pi.GetGetMethod();
                Assert.AreEqual("get_Number", mi.Name);
                Assert.AreEqual("System.Int32", mi.ReturnType.FullName);
                Assert.AreEqual(0, mi.GetParameters().Length);
                Assert.IsTrue(mi.IsPublic);
            }
        }

        [TestMethod]
        public void Test_PropertyDef_GetSetMethod()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(TypeWithProperties).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                PropertyInfo pi = t.GetProperty("Name");
                MethodInfo mi = pi.GetSetMethod();
                Assert.AreEqual("set_Name", mi.Name);
                Assert.AreEqual("System.Void", mi.ReturnType.FullName);
                Assert.AreEqual("System.String", mi.GetParameters()[0].ParameterType.FullName);
                Assert.IsTrue(mi.IsPublic);

                pi = t.GetProperty("Number");
                mi = pi.GetSetMethod();
                Assert.IsNull(mi);
            }
        }

        [TestMethod]
        public void Test_PropertyDef_GetAccessors()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(TypeWithProperties).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                PropertyInfo pi = t.GetProperty("Name");
                MethodInfo[] methods = pi.GetAccessors();
                Assert.AreEqual(2, methods.Length);
                MethodInfo mi;

                mi = methods[0];
                Assert.AreEqual("get_Name", mi.Name);
                Assert.AreEqual("System.String", mi.ReturnType.FullName);
                Assert.AreEqual(0, mi.GetParameters().Length);
                Assert.IsTrue(mi.IsPublic);

                mi = methods[1];
                Assert.AreEqual("set_Name", mi.Name);
                Assert.AreEqual("System.Void", mi.ReturnType.FullName);
                Assert.AreEqual("System.String", mi.GetParameters()[0].ParameterType.FullName);
                Assert.IsTrue(mi.IsPublic);
            }
        }

        [TestMethod]
        public void Test_PropertyDef_GetGetMethod_NonPublic()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(TypeWithProperties).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                MethodInfo mi;
                PropertyInfo pi = t.GetProperty("PrivateArrayProperty");

                mi = pi.GetGetMethod();
                Assert.IsNull(mi);

                mi = pi.GetGetMethod(true);
                Assert.AreEqual("get_PrivateArrayProperty", mi.Name);
                Assert.IsTrue(mi.IsPrivate);
            }
        }

        [TestMethod]
        public void Test_PropertyDef_GetSetMethod_NonPublic()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(TypeWithProperties).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                MethodInfo mi;
                PropertyInfo pi = t.GetProperty("PrivateArrayProperty");

                mi = pi.GetSetMethod();
                Assert.IsNull(mi);

                mi = pi.GetSetMethod(true);
                Assert.AreEqual("set_PrivateArrayProperty", mi.Name);
                Assert.IsTrue(mi.IsPrivate);
            }
        }

        [TestMethod]
        public void Test_PropertyDef_GetAccessors_NonPublic()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(TypeWithProperties).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                PropertyInfo pi = t.GetProperty("PrivateArrayProperty");
                MethodInfo[] methods;

                methods = pi.GetAccessors();
                Assert.AreEqual(0, methods.Length);

                methods = pi.GetAccessors(true);
                Assert.AreEqual(2, methods.Length);
                MethodInfo mi;

                mi = methods[0];
                Assert.AreEqual("get_PrivateArrayProperty", mi.Name);                
                Assert.IsTrue(mi.IsPrivate);

                mi = methods[1];
                Assert.AreEqual("set_PrivateArrayProperty", mi.Name);
                Assert.IsTrue(mi.IsPrivate);
            }
        }
    }
}
