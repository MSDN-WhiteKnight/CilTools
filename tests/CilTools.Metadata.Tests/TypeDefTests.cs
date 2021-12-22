/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Reflection;
using CilTools.Syntax;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    public class SampleType
    {
        public static string PublicStaticField;
        public string PublicInstanceField;
        private static string PrivateStaticField;
        private string PrivateInstanceField;
        public string PublicProperty { get; set; }

        public static void PublicStaticMethod() 
        {
            PrivateStaticField = string.Empty;
            Console.WriteLine(PrivateStaticField);
            PrivateStaticMethod();
        }
        public void PublicInstanceMethod() 
        {
            PrivateInstanceField = string.Empty;
            Console.WriteLine(PrivateInstanceField);
            PrivateInstanceMethod();
        }

        private static void PrivateStaticMethod() { }
        private void PrivateInstanceMethod() { }
    }

    [TestClass]
    public class TypeDefTests
    {
        const string SampleTypeName = "CilTools.Metadata.Tests.SampleType";

        [TestMethod]
        public void Test_TypeDef()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);

                Assert.AreEqual(SampleTypeName, t.FullName);
                Assert.AreEqual("CilTools.Metadata.Tests", t.Namespace);
                Assert.AreEqual("SampleType", t.Name);
                Assert.AreEqual(TypeAttributes.Public, t.Attributes & TypeAttributes.VisibilityMask);

                Assert.IsTrue(t.IsClass);
                Assert.IsTrue(t.IsPublic);
                Assert.IsFalse(t.IsAbstract);
                Assert.IsFalse(t.IsSealed);
                Assert.IsFalse(t.IsInterface);
                Assert.IsFalse(t.IsValueType);
            }
        }

        [TestMethod]
        public void Test_GetMembers_All()
        {
            AssemblyReader reader = new AssemblyReader();
            
            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                MemberInfo[] members = t.GetMembers(Utils.AllMembers());

                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is MethodBase && x.Name == "PublicStaticMethod");
                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is MethodBase && x.Name == "PublicInstanceMethod");
                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is MethodBase && x.Name == "PrivateStaticMethod");
                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is MethodBase && x.Name == "PrivateInstanceMethod");

                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is FieldInfo && x.Name == "PublicStaticField");
                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is FieldInfo && x.Name == "PublicInstanceField");
                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is FieldInfo && x.Name == "PrivateStaticField");
                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is FieldInfo && x.Name == "PrivateInstanceField");

                AssertThat.HasOnlyOneMatch(members, 
                    (x) => x is PropertyInfo && x.Name == "PublicProperty");
            }
        }

        [TestMethod]
        public void Test_GetMembers_Public()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                MemberInfo[] members = t.GetMembers();

                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is MethodBase && x.Name == "PublicStaticMethod");
                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is MethodBase && x.Name == "PublicInstanceMethod");
                AssertThat.HasNoMatches(members,
                    (x) => x is MethodBase && x.Name == "PrivateStaticMethod");
                AssertThat.HasNoMatches(members,
                    (x) => x is MethodBase && x.Name == "PrivateInstanceMethod");

                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is FieldInfo && x.Name == "PublicStaticField");
                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is FieldInfo && x.Name == "PublicInstanceField");
                AssertThat.HasNoMatches(members,
                    (x) => x is FieldInfo && x.Name == "PrivateStaticField");
                AssertThat.HasNoMatches(members,
                    (x) => x is FieldInfo && x.Name == "PrivateInstanceField");
            }
        }

        [TestMethod]
        public void Test_GetMembers_Static()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);

                MemberInfo[] members = t.GetMembers(BindingFlags.Static|BindingFlags.Public|
                    BindingFlags.NonPublic);

                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is MethodBase && x.Name == "PublicStaticMethod");
                AssertThat.HasNoMatches(members,
                    (x) => x is MethodBase && x.Name == "PublicInstanceMethod");
                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is MethodBase && x.Name == "PrivateStaticMethod");
                AssertThat.HasNoMatches(members,
                    (x) => x is MethodBase && x.Name == "PrivateInstanceMethod");

                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is FieldInfo && x.Name == "PublicStaticField");
                AssertThat.HasNoMatches(members,
                    (x) => x is FieldInfo && x.Name == "PublicInstanceField");
                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is FieldInfo && x.Name == "PrivateStaticField");
                AssertThat.HasNoMatches(members,
                    (x) => x is FieldInfo && x.Name == "PrivateInstanceField");
            }
        }

        [TestMethod]
        public void Test_GetMembers_Instance()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);

                MemberInfo[] members = t.GetMembers(BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic);

                AssertThat.HasNoMatches(members,
                    (x) => x is MethodBase && x.Name == "PublicStaticMethod");
                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is MethodBase && x.Name == "PublicInstanceMethod");
                AssertThat.HasNoMatches(members,
                    (x) => x is MethodBase && x.Name == "PrivateStaticMethod");
                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is MethodBase && x.Name == "PrivateInstanceMethod");

                AssertThat.HasNoMatches(members,
                    (x) => x is FieldInfo && x.Name == "PublicStaticField");
                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is FieldInfo && x.Name == "PublicInstanceField");
                AssertThat.HasNoMatches(members,
                    (x) => x is FieldInfo && x.Name == "PrivateStaticField");
                AssertThat.HasOnlyOneMatch(members,
                    (x) => x is FieldInfo && x.Name == "PrivateInstanceField");
            }
        }

        [TestMethod]
        public void Test_TypeDef_SameInstance()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                Type t2 = ass.GetType(SampleTypeName);
                Assert.AreSame(t, t2);
            }
        }

        [TestMethod]
        public void Test_GetProperties()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                PropertyInfo[] props = t.GetProperties(Utils.AllMembers());
                PropertyInfo p = props[0];
                Assert.AreEqual("PublicProperty", p.Name);
            }
        }

        [TestMethod]
        public void Test_GetTypeDefSyntax_Property()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t);
                string s = Utils.SyntaxToString(nodes);

                AssertThat.IsMatch(s, new Text[] {
                    ".class", Text.Any,"public", Text.Any,"SampleType", Text.Any,"{", Text.Any,
                    ".property", Text.Any,"instance", Text.Any,"string", Text.Any,"PublicProperty", Text.Any,"()", Text.Any,
                    "{", Text.Any,
                    ".get", Text.Any,"instance", Text.Any,"string", Text.Any,"get_PublicProperty", Text.Any,"()", Text.Any,
                    ".set", Text.Any,"instance", Text.Any,"void", Text.Any,"set_PublicProperty", Text.Any,"(", Text.Any,
                    "string", Text.Any,")", Text.Any,
                    "}", Text.Any,
                    "}", Text.Any});
            }
        }
        
        [TestMethod]
        public void Test_GetMethods_All()
        {
            AssemblyReader reader = new AssemblyReader();
            
            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                MethodInfo[] members = t.GetMethods(Utils.AllMembers());
                Assert.IsTrue(members.Length>=4);

                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == "PublicStaticMethod");
                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == "PublicInstanceMethod");
                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == "PrivateStaticMethod");
                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == "PrivateInstanceMethod");
            }
        }
        
        [TestMethod]
        public void Test_GetMethods_Public()
        {
            AssemblyReader reader = new AssemblyReader();
            
            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                MethodInfo[] members = t.GetMethods();
                Assert.IsTrue(members.Length>=2);

                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == "PublicStaticMethod");
                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == "PublicInstanceMethod");
                AssertThat.HasNoMatches(members,(x) => x.Name == "PrivateStaticMethod");
                AssertThat.HasNoMatches(members,(x) => x.Name == "PrivateInstanceMethod");
            }
        }
        
        [TestMethod]
        public void Test_GetMethods_Static()
        {
            AssemblyReader reader = new AssemblyReader();
            
            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                MethodInfo[] members = t.GetMethods(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
                Assert.IsTrue(members.Length>=2);

                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == "PublicStaticMethod");
                AssertThat.HasNoMatches(members,(x) => x.Name == "PublicInstanceMethod");
                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == "PrivateStaticMethod");
                AssertThat.HasNoMatches(members,(x) => x.Name == "PrivateInstanceMethod");
            }
        }
        
        [TestMethod]
        public void Test_GetConstructors()
        {
            AssemblyReader reader = new AssemblyReader();
            
            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                ConstructorInfo[] members = t.GetConstructors(Utils.AllMembers());
                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == ".ctor");
            }
        }
    }
}
