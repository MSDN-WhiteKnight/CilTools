/* CilTools.Metadata tests
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.BytecodeAnalysis;
using CilTools.Syntax;
using CilTools.Tests.Common;
using CilTools.Tests.Common.Attributes;
using CilTools.Tests.Common.TestData;
using CilTools.Tests.Common.TextUtils;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class TypeRefTests
    {
        static Type GetTypeRef_System_Int32()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType(typeof(SampleMethods).FullName);
            MethodInfo mi = t.GetMethod("TestTokens");
            CilGraph graph = CilGraph.Create(mi);
            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            CilGraphNode node = nodes.Where((x) => x.Instruction.OpCode == OpCodes.Ldtoken &&
                x.Instruction.ReferencedType.FullName == "System.Int32").First();

            return node.Instruction.ReferencedType;
        }
        
        [TestMethod]
        public void Test_TypeRef()
        {
            Type t = GetTypeRef_System_Int32();

            Assert.AreEqual("System.Int32", t.FullName);
            Assert.AreEqual("System", t.Namespace);
            Assert.AreEqual("Int32", t.Name);
            Assert.IsTrue(t.IsPublic);
            Assert.AreEqual("System.ValueType", t.BaseType.FullName);
            Assert.IsFalse(t.IsClass);
            Assert.IsFalse(t.IsEnum);
            Assert.IsFalse(t.IsInterface);
            Assert.IsFalse(t.IsArray);
            Assert.IsFalse(t.IsByRef);
            Assert.IsFalse(t.IsPointer);
            Assert.IsFalse(t.IsGenericParameter);
            Assert.IsFalse(t.IsGenericType);
            Assert.IsFalse(t.IsGenericTypeDefinition);
            Assert.IsFalse(t.IsNested);
            Assert.IsNull(t.DeclaringType);
        }

        [TestMethod]
        public void Test_IsValueType_Struct()
        {
            Type t = GetTypeRef_System_Int32();
            
            Assert.IsTrue(t.IsValueType);
        }

        [TestMethod]
        public void Test_IsValueType_Class()
        {
            Type t = TypeRefTests_Data.GetTypeRef(typeof(Console).FullName);

            Assert.IsFalse(t.IsValueType);
            Assert.IsTrue(t.IsClass);
            Assert.AreEqual("System.Object", t.BaseType.FullName);
        }

        [TestMethod]
        public void Test_IsValueType_Enum()
        {
            Type t = TypeRefTests_Data.GetTypeRef(typeof(AttributeTargets).FullName);

            Assert.IsTrue(t.IsValueType);
            Assert.IsFalse(t.IsClass);
            Assert.AreEqual("System.Enum", t.BaseType.FullName);
        }

        [TestMethod]
        public void Test_IsValueType_Interface()
        {
            Type t = TypeRefTests_Data.GetTypeRef(typeof(IDisposable).FullName);

            Assert.IsFalse(t.IsValueType);
            Assert.IsTrue(t.IsInterface);
        }

        [TestMethod]
        public void Test_IsEnum()
        {
            Type t = TypeRefTests_Data.GetTypeRef(typeof(AttributeTargets).FullName);
            Assert.IsTrue(t.IsEnum);

            t = TypeRefTests_Data.GetTypeRef(typeof(Console).FullName);
            Assert.IsFalse(t.IsEnum);
        }
    }

    public class TypeRefTests_Data
    {
        public static void Method()
        {
            Console.WriteLine(typeof(AttributeTargets));
            Console.WriteLine(typeof(Console));
            Console.WriteLine(typeof(IDisposable));
        }

        public static Type GetTypeRef(string typeName)
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(TypeRefTests_Data).Assembly.Location);
            Type t = ass.GetType(typeof(TypeRefTests_Data).FullName);
            MethodInfo mi = t.GetMethod("Method");
            CilGraph graph = CilGraph.Create(mi);
            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            CilGraphNode node = nodes.Where((x) => x.Instruction.OpCode == OpCodes.Ldtoken &&
                x.Instruction.ReferencedType.FullName == typeName).First();

            return node.Instruction.ReferencedType;
        }
    }
}
