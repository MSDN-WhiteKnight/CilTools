/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.Reflection;
using CilTools.Tests.Common;
using CilTools.Tests.Common.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    public class MethodDefTests_BaseClass
    {
        public virtual void Foo(int x, int y) { }
        public virtual void Foo(string s) { }
        public virtual void Bar(DateTime dt) { }
    }

    public class MethodDefTests_DerivedClass : MethodDefTests_BaseClass
    {
        public override void Foo(int x, int y) { }
        public override void Foo(string s) { }
        public new void Bar(DateTime dt) { }
        public static void Buzz() { }
        public virtual void Frobby() { }
        public void Bobby() { }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [TestClass]
    public class MethodDefTests
    {
        const string typename = "CilTools.Tests.Common.SampleMethods";

        [TestMethod]
        public void Test_MethodDef_DeclaringType()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typename);
                MethodBase m = t.GetMember("PrintHelloWorld")[0] as MethodBase;
                Type tDecl = m.DeclaringType;
                Assert.AreEqual(typename, tDecl.FullName);
                Assert.IsTrue(tDecl.IsPublic);
                Assert.IsTrue(tDecl.IsClass);
                Assert.AreSame(t, tDecl);
            }
        }

        [TestMethod]
        public void Test_MethodDef_GetExceptionBlocks()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typename);
                ICustomMethod m = t.GetMember("DivideNumbers")[0] as ICustomMethod;
                ExceptionBlock[] blocks=m.GetExceptionBlocks();
                Assert.AreEqual(2, blocks.Length);

                ExceptionBlock catchBlock;
                catchBlock = blocks.Where(x => x.Flags == ExceptionHandlingClauseOptions.Clause).First();
                Type catchType = catchBlock.CatchType;
                Assert.AreEqual("DivideByZeroException", catchType.Name);

                AssertThat.HasOnlyOneMatch(blocks, x => x.Flags == ExceptionHandlingClauseOptions.Finally);
            }
        }

        [TestMethod]
        public void Test_MethodDef_MemberType()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typename);
                MemberInfo m = t.GetMember("PrintHelloWorld")[0];
                Assert.IsTrue(m is MethodInfo);
                Assert.AreEqual(MemberTypes.Method, m.MemberType);
            }
        }

        [TestMethod]
        [WorkItem(94)]
        public void Test_MethodDef_GenericParameterByRef()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(System.Threading.Interlocked).Assembly.Location);
                Type t = ass.GetType("System.Threading.Interlocked");

                //Interlocked.CompareExchange<T>(!!T& location1,!!T value,!!T comparand)
                MethodBase m = (MethodBase)(t.GetMember("CompareExchange").
                    Where(x => x is MethodBase && (x as MethodBase).IsGenericMethod).
                    First());

                Type pt0 = m.GetParameters()[0].ParameterType;
                Assert.IsTrue(pt0.IsByRef);
                Assert.IsFalse(pt0.IsGenericParameter);
                Assert.IsTrue(pt0.GetElementType().IsGenericParameter);
            }
        }

        [TestMethod]
        [WorkItem(53)]
        public void Test_MethodDef_GenericConstraints()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typename);
                MethodBase m = t.GetMethod("Sum");
                Type[] args = m.GetGenericArguments();
                Type tArg = args[0];
                Type[] constrains = tArg.GetGenericParameterConstraints();

                GenericParameterAttributes expected = GenericParameterAttributes.NotNullableValueTypeConstraint |
                    GenericParameterAttributes.DefaultConstructorConstraint;

                Assert.AreEqual(expected, tArg.GenericParameterAttributes);
                Assert.AreEqual(1, constrains.Length);
                Assert.AreEqual("System.ValueType", constrains[0].FullName);
            }
        }

        [TestMethod]
        [WorkItem(53)]
        public void Test_MethodDef_GenericConstraints_None()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typename);
                MethodBase m = t.GetMethod("GenerateArray");
                Type[] args = m.GetGenericArguments();
                Type tArg = args[0];
                Type[] constrains = tArg.GetGenericParameterConstraints();

                Assert.AreEqual(GenericParameterAttributes.None, tArg.GenericParameterAttributes);
                Assert.AreEqual(0, constrains.Length);
            }
        }

        [TestMethod]
        [WorkItem(54)]
        public void Test_ReturnTypeCustomAttributes()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typename);
                MethodInfo m = t.GetMethod("ReturnTypeAttributeTest");
                ICustomAttributeProvider provider = m.ReturnTypeCustomAttributes;
                object[] attrs = provider.GetCustomAttributes(false);

                Assert.AreEqual(1, attrs.Length);
                ICustomAttribute ica = (ICustomAttribute)attrs[0];
                Assert.AreEqual("MyAttribute", ica.Constructor.DeclaringType.Name);
                byte[] data = ica.Data;
                byte[] expectedData = new byte[] { 0x01, 0, 0xe7, 0x03, 0, 0, 0, 0 };
                CollectionAssert.AreEqual(expectedData, data);
            }
        }

        [TestMethod]
        [WorkItem(54)]
        public void Test_ReturnTypeCustomAttributes_Negative()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typename);
                MethodInfo m = t.GetMethod("PrintHelloWorld");
                ICustomAttributeProvider provider = m.ReturnTypeCustomAttributes;
                object[] attrs = provider.GetCustomAttributes(false);

                Assert.AreEqual(0, attrs.Length);
            }
        }

        [TestMethod]
        public void Test_GetBaseDefinition_Same()
        {
            AssemblyReader reader = ReaderFactory.GetReader();

            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType(typename);
            MethodInfo m = t.GetMethod("PrintHelloWorld");
            MethodInfo baseDef = m.GetBaseDefinition();
            Assert.IsNotNull(baseDef);
            Assert.AreSame(m, baseDef);
        }

        [TestMethod]
        public void Test_GetBaseDefinition_Override()
        {
            AssemblyReader reader = ReaderFactory.GetReader();

            Assembly ass = reader.LoadFrom(typeof(MethodDefTests_DerivedClass).Assembly.Location);
            Type t = ass.GetType(typeof(MethodDefTests_DerivedClass).FullName);

            MethodInfo m1 = t.GetMethod("Foo", new Type[] { typeof(int), typeof(int) });
            MethodInfo baseDef1 = m1.GetBaseDefinition();
            Assert.IsNotNull(baseDef1);
            Assert.AreEqual("MethodDefTests_BaseClass", baseDef1.DeclaringType.Name);
            Assert.AreEqual("Foo", baseDef1.Name);
            ParameterInfo[] pars = baseDef1.GetParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("x", pars[0].Name);
            Assert.AreEqual("y", pars[1].Name);

            MethodInfo m2 = t.GetMethod("Foo", new Type[] { typeof(string) });
            MethodInfo baseDef2 = m2.GetBaseDefinition();
            Assert.IsNotNull(baseDef2);
            Assert.AreEqual("MethodDefTests_BaseClass", baseDef2.DeclaringType.Name);
            Assert.AreEqual("Foo", baseDef2.Name);
            pars = baseDef2.GetParameters();
            Assert.AreEqual(1, pars.Length);
            Assert.AreEqual("s", pars[0].Name);

            Assert.AreNotEqual(baseDef1.MetadataToken, baseDef2.MetadataToken);
            Assert.AreNotEqual(baseDef1, baseDef2);
        }

        [TestMethod]
        [DataRow("Bar")] //hide
        [DataRow("Buzz")] //static
        [DataRow("Frobby")] //instance virtual
        [DataRow("Bobby")] //instance non-virtual
        public void Test_GetBaseDefinition_NoOverride(string name)
        {
            AssemblyReader reader = ReaderFactory.GetReader();

            Assembly ass = reader.LoadFrom(typeof(MethodDefTests_DerivedClass).Assembly.Location);
            Type t = ass.GetType(typeof(MethodDefTests_DerivedClass).FullName);

            MethodInfo m = t.GetMethod(name);
            MethodInfo baseDef = m.GetBaseDefinition();
            Assert.IsNotNull(baseDef);
            Assert.AreEqual("MethodDefTests_DerivedClass", baseDef.DeclaringType.Name);
            Assert.AreEqual(name, baseDef.Name);
            Assert.AreSame(m, baseDef);

        }

        [ConditionalTest(TestCondition.NetFrameworkOnly, "TypeRef.GetMethodImpl is not implemented")]
        [WorkItem(127)]
        public void Test_GetBaseDefinition_FromObject()
        {
            //https://github.com/MSDN-WhiteKnight/CilTools/issues/127

            AssemblyReader reader = ReaderFactory.GetReader();

            Assembly ass = reader.LoadFrom(typeof(MethodDefTests_DerivedClass).Assembly.Location);
            Type t = ass.GetType(typeof(MethodDefTests_DerivedClass).FullName);

            MethodInfo m = t.GetMethod("ToString");
            MethodInfo baseDef = m.GetBaseDefinition();
            Assert.IsNotNull(baseDef);
            Assert.AreEqual("System.Object", baseDef.DeclaringType.FullName);
            Assert.AreEqual("ToString", baseDef.Name);

            Assert.AreNotEqual(m.MetadataToken, baseDef.MetadataToken);
            Assert.AreNotEqual(m, baseDef);
        }
    }
}
