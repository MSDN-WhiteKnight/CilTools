/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilTools.Tests.Common;
using CilTools.Tests.Common.TextUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class ReflectionTests
    {
        //reflection tests verify that reflection objects returned by our APIs, such as MethodBase 
        //derived classes, are functional and can be inspected by these APIs again

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_NavigationExternal(MethodBase mi)
        {
            //test that we can obtain a reference to called method in external assembly and then
            //decompile its code
            //this test models what happens in CIL View when user clicks on method reference
            //Input method: SampleMethods.PrintHelloWorld

            CilGraph graph = CilGraph.Create(mi);
            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            //find called Console.WriteLine method
            MethodBase navigated = null;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].Instruction.OpCode.FlowControl == FlowControl.Call)
                {
                    MethodBase target = nodes[i].Instruction.ReferencedMember as MethodBase;

                    if (String.Equals(target.Name, "WriteLine", StringComparison.InvariantCulture))
                    {
                        navigated = target;
                        break;
                    }
                }
            }

            Assert.IsNotNull(navigated, "PrintHelloWorld should contain a call to WriteLine");

            //decompile called method
            graph = CilGraph.Create(navigated);
            nodes = graph.GetNodes().ToArray();
            AssertThat.IsCorrect(graph);
            Assert.IsTrue(nodes.Length > 0, "WriteLine method should not be empty");

            AssertThat.HasAtLeastOneMatch(
                nodes, (x) => x.Instruction.OpCode == OpCodes.Ret,
                "WriteLine method should contain 'ret' instruction");

            string str = graph.ToText();

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any, "public", Text.Any,
                "WriteLine", Text.Any,
                "string", Text.Any,
                "cil", Text.Any, "managed", Text.Any,
                "{", Text.Any,
                "ret", Text.Any,
                "}"
            });

            /*.method  public hidebysig static void WriteLine(
                string value
            ) cil managed noinlining
            {
             .maxstack  8

                      call        class [mscorlib]System.IO.TextWriter [mscorlib]System.Console::get_Out()
                      ldarg.0     
                      callvirt    instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
                      ret         
            }*/
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "CreatePoint", BytecodeProviders.All)]
        public void Test_NavigationInternal(MethodBase mi)
        {
            //test that we can obtain a reference to called method in the same assembly and then
            //decompile its code
            //this test models what happens in CIL View when user clicks on method reference
            //Input method: SampleMethods.CreatePoint

            CilGraph graph = CilGraph.Create(mi);
            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            //find called MyPoint.set_X method
            MethodBase navigated = null;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].Instruction.OpCode.FlowControl == FlowControl.Call)
                {
                    MethodBase target = nodes[i].Instruction.ReferencedMember as MethodBase;

                    if (String.Equals(target.Name, "set_X", StringComparison.InvariantCulture))
                    {
                        navigated = target;
                        break;
                    }
                }
            }

            Assert.IsNotNull(navigated, "CreatePoint should contain a call to set_X");

            //decompile called method
            graph = CilGraph.Create(navigated);
            nodes = graph.GetNodes().ToArray();
            AssertThat.IsCorrect(graph);
            Assert.IsTrue(nodes.Length > 0, "set_X method should not be empty");

            AssertThat.HasAtLeastOneMatch(
                nodes,
                (x) => x.Instruction.OpCode == OpCodes.Stfld &&
                String.Equals(
                    (x.Instruction.ReferencedMember as FieldInfo).FieldType.Name,
                    "Single",
                    StringComparison.InvariantCulture),
                "set_X method should contain 'stfld' instruction referencing 'float32' field");

            string str = graph.ToText();

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any, "public", Text.Any,
                "set_X", Text.Any,
                "float32", Text.Any,
                "cil", Text.Any, "managed", Text.Any,
                "{", Text.Any,
                "ldarg", Text.Any,
                "stfld", Text.Any,"float32", Text.Any,
                "MyPoint", Text.Any,
                "ret", Text.Any,
                "}"
            });

            /*.method  public hidebysig instance void set_X(
                float32 value
            ) cil managed
            {
             .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = 
            ( 01 00 00 00 )
             .maxstack  8

                      ldarg.0     
                      ldarg.1     
                      stfld       float32 [CilTools.Tests.Common]CilTools.Tests.Common.MyPoint::<X>k__BackingField
                      ret         
            }*/
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TypedRefTest", BytecodeProviders.All)]
        public void Test_TypedReferenceParam(MethodBase mi)
        {
            //verify that handling of TypedReference in params is correct

            ParameterInfo[] pars = mi.GetParameters();

            Assert.IsTrue(
                String.Equals(pars[0].ParameterType.Name, "TypedReference", StringComparison.InvariantCulture)
                );

            CilGraph graph = CilGraph.Create(mi);
            string str = graph.ToString();

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any,
                "TypedRefTest", Text.Any,
                "(", Text.Any,
                "typedref", Text.Any,
                ")", Text.Any,
                "cil", Text.Any, "managed", Text.Any
            });

            //.method public hidebysig static void TypedRefTest(typedref tr) cil managed
        }

        [TestMethod]        
        public void Test_GenericParamType()
        {
            GenericParamType tParam = GenericParamType.Create(typeof(IList<>), 0, null);
            Assert.IsTrue(tParam.IsGenericParameter);
            Assert.AreEqual("T", tParam.Name);
            Assert.AreEqual("IList`1", tParam.DeclaringType.Name);
            Assert.IsNull(tParam.DeclaringMethod);

            tParam = GenericParamType.Create(typeof(IList<>), 0, "T");
            Assert.IsTrue(tParam.IsGenericParameter);
            Assert.AreEqual("T", tParam.Name);
            Assert.AreEqual("IList`1", tParam.DeclaringType.Name);
            Assert.IsNull(tParam.DeclaringMethod);
        }

        static void VerifyGenericContext_IList(GenericContext ctx)
        {
            Assert.IsNull(ctx.DeclaringMethod);
            Assert.AreEqual("IList`1", ctx.DeclaringType.Name);
            Assert.AreEqual(0, ctx.MethodArgumentsCount);
            Assert.AreEqual(1, ctx.TypeArgumentsCount);
            Type t = ctx.GetTypeArgument(0);
            Assert.IsTrue(t.IsGenericParameter);
            Assert.AreEqual("T", t.Name);
        }

        [TestMethod]
        public void Test_GenericContext_GenericType()
        {
            GenericContext ctx = GenericContext.Create(typeof(IList<>), null);
            VerifyGenericContext_IList(ctx);

            ctx = GenericContext.FromArgs(typeof(IList<>).GetGenericArguments(), null);
            VerifyGenericContext_IList(ctx);

            ctx = GenericContext.FromArgs(new Type[] { typeof(int) }, null);
            Assert.IsNull(ctx.DeclaringMethod);
            Assert.AreEqual(0, ctx.MethodArgumentsCount);
            Assert.AreEqual(1, ctx.TypeArgumentsCount);
            Type t = ctx.GetTypeArgument(0);
            Assert.IsFalse(t.IsGenericParameter);
            Assert.AreEqual("Int32", t.Name);
        }

        static void VerifyGenericContext_Method(GenericContext ctx,MethodBase m)
        {
            Assert.AreEqual(1, ctx.MethodArgumentsCount);
            Assert.AreEqual(0, ctx.TypeArgumentsCount);
            Assert.IsNull(ctx.DeclaringType);
            Assert.AreEqual(m.Name, ctx.DeclaringMethod.Name);
            Type t = ctx.GetMethodArgument(0);
            Assert.IsTrue(t.IsGenericParameter);
            Assert.AreEqual("T", t.Name);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods),"GenerateArray",BytecodeProviders.All)]
        public void Test_GenericContext_GenericMethod(MethodBase m)
        {
            GenericContext ctx = GenericContext.Create(null, m);
            VerifyGenericContext_Method(ctx, m);

            ctx = GenericContext.FromArgs(null, m.GetGenericArguments());
            VerifyGenericContext_Method(ctx, m);

            ctx = GenericContext.FromArgs(null, new Type[] { typeof(int) });
            Assert.AreEqual(1, ctx.MethodArgumentsCount);
            Assert.AreEqual(0, ctx.TypeArgumentsCount);
            Assert.IsNull(ctx.DeclaringType);
            Assert.IsNotNull(ctx.DeclaringMethod);
            Type t = ctx.GetMethodArgument(0);
            Assert.IsFalse(t.IsGenericParameter);
            Assert.AreEqual("Int32", t.Name);
        }
    }
}
