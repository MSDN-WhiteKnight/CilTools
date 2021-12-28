/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilTools.Syntax;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class MethodSpecTests
    {
        static MethodBase GetMethodSpec()
        {
            Assembly ass = ReaderFactory.GetReader().LoadFrom(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mb = t.GetMember("GenericsTest")[0] as MethodBase;

            CilGraph graph = CilGraph.Create(mb);
            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            //find called GenerateArray method
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

            Assert.IsNotNull(called, "GenericsTest should contain a call to GenerateArray");
            return called;
        }

        [TestMethod]
        public void Test_MethodSpec_GenericMethodParameter()
        {
            //Verify that generic method parameter operand does not lose its connection with generic 
            //method definition when obtained from instantiation
            MethodBase m = GetMethodSpec();

            CilGraph graph = CilGraph.Create(m);
            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            //newarr !!T
            CilGraphNode node = nodes.Where((x) => x.Instruction.OpCode == OpCodes.Newarr).Single();
            Type t = node.Instruction.ReferencedType;

            Assert.IsTrue(t.IsGenericParameter);
            Assert.AreEqual(0, t.GenericParameterPosition);
            Assert.AreEqual("GenerateArray", t.DeclaringMethod.Name);
            Assert.AreEqual("T", t.Name);
        }

        [TestMethod]
        public void Test_MethodSpec_GetGenericArguments()
        {
            MethodBase m = GetMethodSpec();
            Type[] args = m.GetGenericArguments();
            Assert.AreEqual(1, args.Length);
            Assert.IsFalse(args[0].IsGenericParameter);
            Assert.AreEqual("System.Int32", args[0].FullName);
        }
    }
}
