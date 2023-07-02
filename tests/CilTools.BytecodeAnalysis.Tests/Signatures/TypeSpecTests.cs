/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Reflection;
using CilTools.Tests.Common;

namespace CilTools.BytecodeAnalysis.Tests.Signatures
{
    public class SampleGenericType<U> 
    {
        public static U[] GenerateArray(int n) 
        {
            return new U[n];
        }
    }

    public class SampleConsumingType 
    {
        public static string[] Foo() 
        {
            return SampleGenericType<string>.GenerateArray(10);
        }
    }

    [TestClass]
    public class TypeSpecTests
    {
        static void VerifyInt32(TypeSpec ts)
        {
            Assert.AreEqual(ElementType.I4, ts.ElementType);
            Assert.IsFalse(ts.IsPinned);
            Assert.IsFalse(ts.IsByRef);
            Assert.IsFalse(ts.IsArray);
            Assert.IsFalse(ts.IsPointer);
            Assert.IsFalse(ts.IsGenericType);
            Assert.IsTrue(ts.IsValueType);
            Assert.IsTrue(ts.IsPublic);
            Assert.AreEqual(0, ts.ModifiersCount);
            Assert.AreEqual("System.Int32", ts.FullName);
        }

        [TestMethod]
        public void Test_ReadFromArray_Int()
        {
            byte[] data = new byte[] { 0x08 };
            GenericContext gctx = GenericContext.Create((Type)null, null);
            SignatureContext ctx = SignatureContext.Create(MockTokenResolver.Value, gctx, null);
            TypeSpec ts = TypeSpec.ReadFromArray(data, ctx);
            VerifyInt32(ts);
        }

        [TestMethod]        
        public void Test_ReadFromArray_GenericTypeParam()
        {
            byte[] data = new byte[] { 0x13, 0x0 };
            GenericContext gctx = GenericContext.Create(typeof(IList<>), null);
            SignatureContext ctx = SignatureContext.Create(MockTokenResolver.Value, gctx, null);
            TypeSpec ts = TypeSpec.ReadFromArray(data, ctx);
            Assert.IsTrue(ts.IsGenericParameter);
            Assert.AreEqual("T", ts.Name);
            Assert.AreEqual("IList`1", ts.DeclaringType.Name);
            Assert.IsNull(ts.DeclaringMethod);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "GenerateArray", BytecodeProviders.All)]
        public void Test_ReadFromArray_GenericMethodParam(MethodBase m)
        {
            byte[] data = new byte[] { 0x1e, 0x0 };
            GenericContext gctx = GenericContext.Create(null, m);
            SignatureContext ctx = SignatureContext.Create(MockTokenResolver.Value, gctx, null);
            TypeSpec ts = TypeSpec.ReadFromArray(data, ctx);
            Assert.IsTrue(ts.IsGenericParameter);
            Assert.AreEqual("T", ts.Name);
            Assert.AreEqual(m.Name, ts.DeclaringMethod.Name);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleConsumingType), "Foo", BytecodeProviders.Metadata)]
        public void Test_GenericTypeParam_Instantiation(MethodBase m)
        {
            //Verify that generic type parameter operand does not lose its connection with generic 
            //type definition when obtained from instantiation
            CilGraph graph = CilGraph.Create(m);
            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            //find called SampleGenericType<string>.GenerateArray method
            MethodBase called = null;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].Instruction.OpCode.FlowControl == FlowControl.Call)
                {
                    MethodBase target = nodes[i].Instruction.ReferencedMember as MethodBase;

                    if (string.Equals(target.Name, "GenerateArray", StringComparison.InvariantCulture))
                    {
                        called = target;
                        break;
                    }
                }
            }

            Assert.IsNotNull(called, "SampleConsumingType.Foo should contain a call to GenerateArray");

            //inspect instructions of GenerateArray
            graph = CilGraph.Create(called);
            nodes = graph.GetNodes().ToArray();

            //newarr !T
            CilGraphNode node = nodes.Where((x) => x.Instruction.OpCode == OpCodes.Newarr).Single();
            
            //finally verify the type object we are interested in
            Type t = node.Instruction.ReferencedType;
            Assert.IsTrue(t.IsGenericParameter);
            Assert.AreEqual(0, t.GenericParameterPosition);
            Assert.IsNull(t.DeclaringMethod);
            Assert.AreEqual(typeof(SampleGenericType<>).FullName, t.DeclaringType.FullName);
            Assert.AreEqual("U", t.Name);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TestByRefTypes", BytecodeProviders.Metadata)]
        public void Test_TypeSpec_ByRefTypes(MethodBase m)
        {
            ParameterInfo[] pars = m.GetParameters();
            Type t = pars[0].ParameterType;
            Type t2 = pars[1].ParameterType;

            //ref to struct
            Assert.IsTrue(t.IsByRef);
            Type elementType = t.GetElementType();
            Assert.IsFalse(elementType.IsByRef);
            Assert.IsTrue(elementType.IsValueType);
            Assert.IsFalse(elementType.IsClass);
            Assert.AreEqual("System.DateTime", elementType.FullName);

            //ref to class
            Assert.IsTrue(t2.IsByRef);
            elementType = t2.GetElementType();
            Assert.IsFalse(elementType.IsByRef);
            Assert.IsFalse(elementType.IsValueType);
            Assert.IsTrue(elementType.IsClass);
            Assert.AreEqual("System.Attribute", elementType.FullName);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TestGenericStruct", BytecodeProviders.Metadata)]
        public void Test_TypeSpec_GenericStruct(MethodBase m)
        {
            ParameterInfo[] pars = m.GetParameters();
            Type t = pars[0].ParameterType;

            Assert.IsTrue(t.IsValueType);
            Assert.IsFalse(t.IsClass);
            Assert.IsTrue(t.IsGenericType);
            Assert.IsFalse(t.IsGenericTypeDefinition);
            Assert.AreEqual(typeof(ArraySegment<>).FullName, t.FullName);
            Assert.AreEqual("System.Int32", t.GetGenericArguments()[0].FullName);
        }

        [TestMethod]
        [MethodTestData(typeof(Enumerable), "ToList", BytecodeProviders.Metadata)]
        public void Test_TypeSpec_GenericClass(MethodInfo m)
        {
            Type t = m.ReturnType;

            Assert.IsFalse(t.IsValueType);
            Assert.IsTrue(t.IsClass);
            Assert.IsTrue(t.IsGenericType);
            Assert.IsFalse(t.IsGenericTypeDefinition);
            Assert.AreEqual(typeof(List<>).FullName, t.FullName);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "CalcSum", BytecodeProviders.Metadata)]
        public void Test_IsValueType_Double(MethodBase m)
        {
            ParameterInfo[] pars = m.GetParameters();
            Type t = pars[0].ParameterType;
            Type t2 = pars[1].ParameterType;

            Assert.IsTrue(t.IsValueType);
            Assert.AreEqual("System.Double", t.FullName);
            Assert.IsTrue(t2.IsValueType);
            Assert.AreEqual("System.Double", t2.FullName);
        }

        [TestMethod]
        public void Test_CreateSpecialType_Int()
        {
            TypeSpec ts = TypeSpec.CreateSpecialType(ElementType.I4);
            VerifyInt32(ts);
        }

        [TestMethod]
        public void Test_CreateSpecialType_String()
        {
            TypeSpec ts = TypeSpec.CreateSpecialType(ElementType.String);
            AssertThat.AreEqual("System.String", ts.FullName);
            Assert.IsFalse(ts.IsValueType);
        }

        [TestMethod]
        public void Test_CreateSpecialType_Negative()
        {
            AssertThat.Throws<ArgumentException>(() => TypeSpec.CreateSpecialType(ElementType.Class));
            AssertThat.Throws<ArgumentException>(() => TypeSpec.CreateSpecialType(ElementType.ValueType));
        }
    }
}
