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
        [TestMethod]
        [WorkItem(92)]
        public void Test_MethodRef_GenericParameterInSignature()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                //load MethodRef
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase m = t.GetMember("TestMethodRefGenericParameter")[0] as MethodBase;

                CilInstruction instr = CilReader.GetInstructions(m).
                    Where(x=>x.Name == "call").First();

                MethodBase mCalled = instr.ReferencedMember as MethodBase;

                //Interlocked.CompareExchange<T>(!!T& location1,!!T value,!!T comparand)
                MethodBase mRef = (mCalled as ICustomMethod).GetDefinition();
                
                //verify that implementation method resolves correctly
                byte[] bytecode = (mRef as ICustomMethod).GetBytecode();
                Assert.IsTrue(bytecode.Length > 0);

                ParameterInfo[] pars = mRef.GetParameters();
                Assert.AreEqual("location1", pars[0].Name);
                Assert.AreEqual("value", pars[1].Name);
                Assert.AreEqual("comparand", pars[2].Name);
            }
        }
    }
}
