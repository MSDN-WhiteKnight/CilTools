/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Syntax;
using CilTools.Tests.Common;
using CilTools.Tests.Common.TestData;
using CilTools.Tests.Common.TextUtils;
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

    public class TypeWithStaticCtor
    {
        static TypeWithStaticCtor() { }
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
                Assert.IsTrue(members.Length == 6);

                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == "PublicStaticMethod");
                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == "PublicInstanceMethod");
                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == "PrivateStaticMethod");
                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == "PrivateInstanceMethod");
                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == "get_PublicProperty");
                AssertThat.HasOnlyOneMatch(members,(x) => x.Name == "set_PublicProperty");
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
                Assert.AreEqual(1, members.Length);
                Assert.AreEqual(".ctor", members[0].Name);
                Assert.IsFalse(members[0].IsStatic);
            }
        }

        [TestMethod]
        public void Test_GetMethod_Public()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                const string name = "PublicStaticMethod";
                MethodInfo m = t.GetMethod(name);
                Assert.IsNotNull(m);
                Assert.AreEqual(name, m.Name);

                m = t.GetMethod(name, new Type[0]);
                Assert.IsNotNull(m);
                Assert.AreEqual(name, m.Name);
            }
        }

        [TestMethod]
        public void Test_GetMethod_Private()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                const string name = "PrivateStaticMethod";
                MethodInfo m = t.GetMethod(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
                Assert.IsNotNull(m);
                Assert.AreEqual(name, m.Name);
            }
        }

        [TestMethod]
        public void Test_GetMethod_NonExisting()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);

                //wrong name
                MethodInfo m = t.GetMethod("FooBarBuzz");
                Assert.IsNull(m);

                //wrong binding flags
                m = t.GetMethod("PublicStaticMethod", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNull(m);

                //wrong calling convention
                m = t.GetMethod("PublicStaticMethod", Utils.AllMembers(), null, CallingConventions.VarArgs, new Type[0], null);
                Assert.IsNull(m);
            }
        }

        [TestMethod]
        public void Test_GetMethod_Overload()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(Console).Assembly.Location);
                Type t = ass.GetType("System.Console");
                const string name = "Write";

                //positive
                MethodInfo m = t.GetMethod(name, new Type[] { typeof(string) });
                Assert.IsNotNull(m);
                Assert.AreEqual(name, m.Name);
                ParameterInfo[] pars = m.GetParameters();
                Assert.AreEqual(1, pars.Length);
                Assert.AreEqual("System.String", pars[0].ParameterType.FullName);

                //negative
                m = t.GetMethod(name, new Type[] { typeof(byte), typeof(int) });
                Assert.IsNull(m);

                //ambiguous
                AssertThat.Throws<AmbiguousMatchException>(() => t.GetMethod(name));
            }
        }

        [TestMethod]
        public void Test_GetConstructor_Parameterless()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);
                ConstructorInfo c = t.GetConstructor(new Type[0]);
                Assert.IsNotNull(c);
                Assert.AreEqual(".ctor", c.Name);
                Assert.AreEqual(MemberTypes.Constructor, c.MemberType);
                Assert.IsFalse(c.IsStatic);
                Assert.AreEqual(SampleTypeName, c.DeclaringType.FullName);
            }
        }

        [TestMethod]
        public void Test_GetConstructor_NonExisting()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
                Type t = ass.GetType(SampleTypeName);

                //wrong signature
                ConstructorInfo c = t.GetConstructor(new Type[] { typeof(int) });
                Assert.IsNull(c);

                //wrong binding flags
                c = t.GetConstructor(BindingFlags.NonPublic|BindingFlags.Static,null,new Type[0], null);
                Assert.IsNull(c);
            }
        }

        [TestMethod]
        public void Test_GetConstructor_WithParams()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(string).Assembly.Location);
                Type t = ass.GetType("System.String");

                //correct
                ConstructorInfo c = t.GetConstructor(new Type[] { typeof(char[]) });
                Assert.IsNotNull(c);
                Assert.AreEqual(".ctor", c.Name);
                Assert.IsFalse(c.IsStatic);

                //incorrect
                c = t.GetConstructor(new Type[] { typeof(byte), typeof(int) });
                Assert.IsNull(c);
            }
        }

        [TestMethod]
        public void Test_GetConstructor_Static()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(TypeWithStaticCtor).Assembly.Location);
                Type t = ass.GetType(typeof(TypeWithStaticCtor).FullName);

                ConstructorInfo c = t.GetConstructor(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null);

                Assert.IsNotNull(c);
                Assert.AreEqual(".cctor", c.Name);
                Assert.AreEqual(MemberTypes.Constructor, c.MemberType);
                Assert.IsTrue(c.IsSpecialName);
                Assert.IsTrue(c.IsStatic);
            }
        }

        [TestMethod]
        public void Test_GetInterfaces()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(InterfacesSampleType).Assembly.Location);
            Type t = ass.GetType(typeof(InterfacesSampleType).FullName);
            Type[] ifTypes = t.GetInterfaces();

            Assert.AreEqual(2, ifTypes.Length);
            AssertThat.HasOnlyOneMatch(ifTypes, (x) => x.FullName == typeof(ITest).FullName);
            AssertThat.HasOnlyOneMatch(ifTypes, (x) => x.FullName == typeof(IComparable).FullName);
            AssertThat.AllMatch(ifTypes, (x) => x.IsInterface);
            AssertThat.AllMatch(ifTypes, (x) => !x.IsClass);
            AssertThat.AllMatch(ifTypes, (x) => !x.IsValueType);
            AssertThat.AllMatch(ifTypes, (x) => x.BaseType == null);
        }

        [TestMethod]
        public void Test_GetInterfaces_Negative()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(SampleType).Assembly.Location);
            Type t = ass.GetType(typeof(SampleType).FullName);
            Type[] ifTypes = t.GetInterfaces();

            Assert.AreEqual(0, ifTypes.Length);
        }

        [TestMethod]
        public void Test_GetInterfaces_External()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(List<>).Assembly.Location);
            Type tList = ass.GetType(typeof(List<>).FullName);
            Type[] ifTypes = tList.GetInterfaces();

            AssertThat.HasOnlyOneMatch(ifTypes, (x) => x.Name == "IEnumerable");
            AssertThat.HasOnlyOneMatch(ifTypes, (x) => x.Name == "IList");
            AssertThat.HasOnlyOneMatch(ifTypes, (x) => x.Name == "ICollection");
            AssertThat.HasOnlyOneMatch(ifTypes, (x) => x.Name == "IEnumerable`1");
            AssertThat.HasOnlyOneMatch(ifTypes, (x) => x.Name == "IList`1");
            AssertThat.HasOnlyOneMatch(ifTypes, (x) => x.Name == "ICollection`1");
            AssertThat.AllMatch(ifTypes, (x) => x.IsInterface);
        }

        void VerifyInterfaceMap(InterfaceMapping map, Type tInterface, Type tTarget)
        {
            Assert.AreSame(tTarget, map.TargetType);
            Assert.AreSame(tInterface, map.InterfaceType);
            Assert.AreEqual(map.TargetMethods.Length, map.InterfaceMethods.Length);

            for (int i = 0; i < map.TargetMethods.Length; i++)
            {
                Assert.AreEqual(tTarget.FullName, map.TargetMethods[i].DeclaringType.FullName);
                Assert.AreEqual(tInterface.FullName, map.InterfaceMethods[i].DeclaringType.FullName);
                Assert.IsTrue(map.InterfaceMethods[i].DeclaringType.IsInterface);
            }
        }

        [TestMethod]
        public void Test_GetInterfaceMap()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(InterfacesSampleType).Assembly.Location);
            Type t = ass.GetType(typeof(InterfacesSampleType).FullName);
            Type tInterface = typeof(ITest);
            InterfaceMapping map = t.GetInterfaceMap(tInterface);
                        
            Assert.AreEqual(2, map.InterfaceMethods.Length);
            Assert.AreEqual(2, map.TargetMethods.Length);
            VerifyInterfaceMap(map, tInterface, t);

            Assert.AreEqual("Foo", map.InterfaceMethods[0].Name);
            Assert.AreEqual("Bar", map.InterfaceMethods[1].Name);
        }

        [TestMethod]
        public void Test_GetInterfaceMap_ExternalInterface()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(InterfacesSampleType).Assembly.Location);
            Type t = ass.GetType(typeof(InterfacesSampleType).FullName);
            Type tInterface = typeof(IComparable);
            InterfaceMapping map = t.GetInterfaceMap(tInterface);

            Assert.AreEqual(1, map.InterfaceMethods.Length);
            Assert.AreEqual(1, map.TargetMethods.Length);
            VerifyInterfaceMap(map, tInterface, t);

            Assert.AreEqual("CompareTo", map.InterfaceMethods[0].Name);
            Assert.AreEqual(typeof(IComparable).FullName, map.InterfaceMethods[0].DeclaringType.FullName);
            Assert.AreEqual(typeof(InterfacesSampleType).FullName, map.TargetMethods[0].DeclaringType.FullName);
        }

        [TestMethod]
        public void Test_GetInterfaceMap_ExternalClass()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(string).Assembly.Location);
            Type t = ass.GetType(typeof(string).FullName);
            Type tInterface = typeof(IEnumerable);
            InterfaceMapping map = t.GetInterfaceMap(tInterface);

            Assert.IsTrue(map.InterfaceMethods.Length > 0);
            VerifyInterfaceMap(map, tInterface, t);
        }

        [TestMethod]
        public void Test_GetInterfaceMap_Exceptions()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(InterfacesSampleType).Assembly.Location);
            Type t = ass.GetType(typeof(InterfacesSampleType).FullName);

            AssertThat.Throws<ArgumentNullException>(() => t.GetInterfaceMap(null));
            AssertThat.Throws<ArgumentException>(() => t.GetInterfaceMap(typeof(SampleMethods)));
            AssertThat.Throws<ArgumentException>(() => t.GetInterfaceMap(typeof(IEnumerable)));
        }
    }
}
