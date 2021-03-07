/* CIL Tools tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.BytecodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Tests.Common
{
    public static class ReflectionTestsCore
    {
        //reflection tests verify that reflection objects returned by our APIs, such as MethodBase 
        //derived classes, are functional and can be inspected by these APIs again

        public static void Test_NavigationExternal(MethodBase mb)
        {
            //test that we can obtain a reference to called method in external assembly and then
            //decompile its code
            //this test models what happens in CIL View when user clicks on method reference
            //Input method: SampleMethods.PrintHelloWorld

            CilGraph graph = CilGraph.Create(mb);
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

            AssertThat.IsMatch(str, new MatchElement[] {
                new Literal(".method"), MatchElement.Any, new Literal("public"), MatchElement.Any,
                new Literal("WriteLine"), MatchElement.Any,
                new Literal("string"), MatchElement.Any,
                new Literal("cil"), MatchElement.Any, new Literal("managed"), MatchElement.Any,
                new Literal("{"), MatchElement.Any,
                new Literal("ret"), MatchElement.Any,
                new Literal("}")
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
        }//end Test

        public static void Test_NavigationInternal(MethodBase mb)
        {
            //test that we can obtain a reference to called method in the same assembly and then
            //decompile its code
            //this test models what happens in CIL View when user clicks on method reference
            //Input method: SampleMethods.CreatePoint

            CilGraph graph = CilGraph.Create(mb);
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

            AssertThat.IsMatch(str, new MatchElement[] {
                new Literal(".method"), MatchElement.Any, new Literal("public"), MatchElement.Any,
                new Literal("set_X"), MatchElement.Any,
                new Literal("float32"), MatchElement.Any,
                new Literal("cil"), MatchElement.Any, new Literal("managed"), MatchElement.Any,
                new Literal("{"), MatchElement.Any,
                new Literal("ldarg"), MatchElement.Any,
                new Literal("stfld"), MatchElement.Any,new Literal("float32"), MatchElement.Any,
                new Literal("MyPoint"), MatchElement.Any,
                new Literal("ret"), MatchElement.Any,
                new Literal("}")
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
        }//end Test

        public static void Test_TypedReferenceParam(MethodBase m)
        {
            //verify that handling of TypedReference in params is correct

            ParameterInfo[] pars = m.GetParameters();

            Assert.IsTrue(
                String.Equals(pars[0].ParameterType.Name, "TypedReference", StringComparison.InvariantCulture)
                );

            CilGraph graph = CilGraph.Create(m);
            string str = graph.ToString();

            AssertThat.IsMatch(str, new MatchElement[] {
                new Literal(".method"), MatchElement.Any,
                new Literal("TypedRefTest"), MatchElement.Any,
                new Literal("("), MatchElement.Any,
                new Literal("typedref"), MatchElement.Any,
                new Literal(")"), MatchElement.Any,
                new Literal("cil"), MatchElement.Any, new Literal("managed"), MatchElement.Any
            });

            //.method public hidebysig static void TypedRefTest(typedref tr) cil managed
        }

        public static void Test_NavigationGenericMethod(MethodBase mb)
        {
            CilGraph graph = CilGraph.Create(mb);
            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            //find called GenerateArray method
            MethodBase navigated = null;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].Instruction.OpCode.FlowControl == FlowControl.Call)
                {
                    MethodBase target = nodes[i].Instruction.ReferencedMember as MethodBase;

                    if (String.Equals(target.Name, "GenerateArray", StringComparison.InvariantCulture))
                    {
                        navigated = target;
                        break;
                    }
                }
            }

            Assert.IsNotNull(navigated, "GenericsTest should contain a call to GenerateArray");

            //decompile called method
            graph = CilGraph.Create(navigated);
            nodes = graph.GetNodes().ToArray();
            AssertThat.IsCorrect(graph);
            string str = graph.ToText();

            AssertThat.HasOnlyOneMatch(
                nodes,
                (x) => x.Instruction.OpCode == OpCodes.Newarr
                && x.Instruction.ReferencedType.IsGenericParameter == true
                && x.Instruction.ReferencedType.GenericParameterPosition == 0
                );

            AssertThat.IsMatch(str, new MatchElement[] {
                new Literal(".method"), MatchElement.Any,
                new Literal("GenerateArray"), MatchElement.Any,
                new Literal("newarr"), MatchElement.Any,
                new Literal("!!"), MatchElement.Any
            });

            /*
            .method   public hidebysig static !!0[] GenerateArray<T>(
                int32 len
            ) cil managed
            {
             .maxstack   1
             .locals   init (!!0[] V_0)

                      nop          
                      ldarg.0      
                      newarr       !!0
                      stloc.0      
                      br.s         IL_0001
             IL_0001: ldloc.0      
                      ret          
            }*/
        }//end test
    }
}
