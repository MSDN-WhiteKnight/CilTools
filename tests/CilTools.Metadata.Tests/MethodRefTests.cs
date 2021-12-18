/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class MethodRefTests
    {
        static readonly AssemblyReader reader = new AssemblyReader();

        static MethodBase GetMethodRef_Interlocked_CompareExchange()
        {
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase m = t.GetMember("TestMethodRefGenericParameter")[0] as MethodBase;

            CilInstruction instr = CilReader.GetInstructions(m).
                Where(x => x.Name == "call").First();

            MethodBase mCalled = instr.ReferencedMember as MethodBase;
            return (mCalled as ICustomMethod).GetDefinition();
        }

        static MethodBase GetMethodRef_Console_WriteLine()
        {
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase m = t.GetMember("PrintHelloWorld")[0] as MethodBase;

            CilInstruction instr = CilReader.GetInstructions(m).
                Where(x => x.Name == "call").First();

            MethodBase mCalled = instr.ReferencedMember as MethodBase;
            return mCalled;
        }

        static MethodBase GetConstructorRef()
        {
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase m = t.GetMember("PrintList")[0] as MethodBase;

            CilInstruction instr = CilReader.GetInstructions(m).
                Where(x => x.Name == "newobj").First();

            MethodBase mCalled = instr.ReferencedMember as MethodBase;
            return mCalled;
        }

        [TestMethod]
        public void Test_MethodRef_IsGenericMethod_Positive()
        {
            MethodBase mRef = GetMethodRef_Interlocked_CompareExchange();
            Assert.IsTrue(mRef.IsGenericMethod);
        }

        [TestMethod]
        public void Test_MethodRef_IsGenericMethod_Negative()
        {
            MethodBase mRef = GetMethodRef_Console_WriteLine();
            Assert.IsFalse(mRef.IsGenericMethod);
        }

        [TestMethod]
        [WorkItem(92)]
        public void Test_MethodRef_GenericParameterInSignature()
        {
            //Interlocked.CompareExchange<T>(!!T& location1,!!T value,!!T comparand)
            MethodBase mRef = GetMethodRef_Interlocked_CompareExchange();

            //verify that implementation method resolves correctly
            byte[] bytecode = (mRef as ICustomMethod).GetBytecode();
            Assert.IsTrue(bytecode.Length > 0);

            ParameterInfo[] pars = mRef.GetParameters();
            Assert.AreEqual("location1", pars[0].Name);
            Assert.AreEqual("value", pars[1].Name);
            Assert.AreEqual("comparand", pars[2].Name);
        }

        [TestMethod]
        public void Test_ConstructorRef_IsGenericMethod()
        {
            MethodBase mRef = GetConstructorRef();
            Assert.IsFalse(mRef.IsGenericMethod);
        }
    }
}
