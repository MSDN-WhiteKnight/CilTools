/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.BytecodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class CilReaderTests
    {
        [TestMethod]
        public void Test_CilReader_HelloWorld()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            CilInstructionBase[] instructions = CilReader.GetInstructions(mi).ToArray();
                        
            AssertThat.NotEmpty(instructions, "The result of PrintHelloWorld method parsing should not be empty collection");
                        
            AssertThat.HasOnlyOneMatch(
                instructions,
                (x) => x.OpCode == OpCodes.Ldstr && x.ReferencedString == "Hello, World",
                "The result of PrintHelloWorld method parsing should contain a single 'ldstr' instruction referencing \"Hello, World\" literal"
                );            

            //verify that instruction sequence contains a single call to Console.WriteLine
            AssertThat.HasOnlyOneMatch(
                instructions,
                (x) => {
                    if (x.OpCode != OpCodes.Call) return false;
                    var method = x.ReferencedMember as MethodBase;
                    if (method == null) return false;

                    return method.Name == "WriteLine" && method.DeclaringType.Name == "Console";
                },
                "The result of PrintHelloWorld method parsing should contain a single call to Console.WriteLine"
                );

            Assert.IsTrue(instructions[instructions.Length - 1].OpCode == OpCodes.Ret, "The last instruction of PrintHelloWorld method should be 'ret'");

            //Test EmitTo: only NetFX
#if !NETSTANDARD
            DynamicMethod dm = new DynamicMethod("PrintHelloWorldDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module);
            ILGenerator ilg = dm.GetILGenerator(512);
            for (int i = 0; i < instructions.Length; i++)
            {
                instructions[i].EmitTo(ilg);
            }
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
#endif
            
        }

        [TestMethod]
        public void Test_CilReader_CalcSum()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("CalcSum");
            CilInstructionBase[] instructions = CilReader.GetInstructions(mi).ToArray();
            
            AssertThat.NotEmpty(instructions, "The result of CalcSum method parsing should not be empty collection");

            AssertThat.HasAtLeastOneMatch(
                instructions,
                (x) => x.OpCode == OpCodes.Ldarg_0 || x.OpCode == OpCodes.Ldarg || x.OpCode == OpCodes.Ldarg_S,
                "The result of CalcSum method parsing should contain at least one instruction loading first argument");

            AssertThat.HasAtLeastOneMatch(
                instructions,
                (x) => x.OpCode == OpCodes.Ldarg_1 || x.OpCode == OpCodes.Ldarg || x.OpCode == OpCodes.Ldarg_S,
                "The result of CalcSum method parsing should contain at least one instruction loading second argument");
            
            AssertThat.HasOnlyOneMatch(
                instructions,
                (x) => x.OpCode == OpCodes.Add,
                "The result of CalcSum method parsing should contain a single 'add' instruction"
                );

            Assert.IsTrue(instructions[instructions.Length - 1].OpCode == OpCodes.Ret, "The last instruction of CalcSum method should be 'ret'");

            //Test EmitTo: only NetFX
            //Dont' run in debug, because compiler generates branching here for some reason
#if !NETSTANDARD && !DEBUG
            DynamicMethod dm = new DynamicMethod("CalcSumDynamic", typeof(double), new Type[] { typeof(double), typeof(double) }, typeof(SampleMethods).Module);
            ILGenerator ilg = dm.GetILGenerator(512);
            for (int i = 0; i < instructions.Length; i++)
            {
                instructions[i].EmitTo(ilg);
            }
            var deleg = (Func<double, double, double>)dm.CreateDelegate(typeof(Func<double, double, double>));
            double result = deleg(1.1,2.4);
            Assert.AreEqual(1.1 + 2.4, result, 0.01, "The result of CalcSumDynamic is wrong");
#endif
        }        

        [TestMethod]
        public void Test_CilReader_StaticFieldAccess()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("SquareFoo");
            CilInstructionBase[] instructions = CilReader.GetInstructions(mi).ToArray();
                        
            AssertThat.NotEmpty(instructions, "The result of SquareFoo method parsing should not be empty collection");
            
            AssertThat.HasOnlyOneMatch(
                instructions,
                (x) => x.OpCode == OpCodes.Mul,
                "The result of SquareFoo method parsing should contain a single 'mul' instruction"
                );

            AssertThat.HasAtLeastOneMatch(
                instructions,
                (x) => x.OpCode == OpCodes.Ldsfld && ((FieldInfo)x.ReferencedMember).Name == "Foo",
                "The result of SquareFoo method parsing should contain at least one 'ldsfld Foo' instruction"
                );

            AssertThat.HasOnlyOneMatch(
                instructions,
                (x) => x.OpCode == OpCodes.Stsfld && ((FieldInfo)x.ReferencedMember).Name == "Foo",
                "The result of SquareFoo method parsing should contain a single 'stsfld Foo' instruction"
                );

            Assert.IsTrue(instructions[instructions.Length - 1].OpCode == OpCodes.Ret, "The last instruction of SquareFoo method should be 'ret'");

            //Test EmitTo: only NetFX
#if !NETSTANDARD
            DynamicMethod dm = new DynamicMethod("SquareFooDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module);
            ILGenerator ilg = dm.GetILGenerator(512);
            for (int i = 0; i < instructions.Length; i++)
            {
                instructions[i].EmitTo(ilg);
            }
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
            Assert.AreEqual(4, SampleMethods.Foo, "The value of SampleMethods.Foo is wrong");
#endif
        }

        [TestMethod]
        public void Test_CilReader_VirtualCall()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("GetInterfaceCount");
            CilInstructionBase[] instructions = CilReader.GetInstructions(mi).ToArray();

            AssertThat.NotEmpty(instructions, "The result of GetInterfaceCount method parsing should not be empty collection");

            AssertThat.HasOnlyOneMatch(
                instructions,
                (x) => x.OpCode == OpCodes.Callvirt && ((MethodBase)x.ReferencedMember).Name == "GetInterfaces",
                "The result of GetInterfaceCount method parsing should contain a single 'callvirt' instruction"
                );

            Assert.IsTrue(instructions[instructions.Length - 1].OpCode == OpCodes.Ret, "The last instruction of GetInterfaceCount method should be 'ret'");

            //Test EmitTo: only NetFX
            //Dont' run in debug, because compiler generates branching here for some reason
#if !NETSTANDARD && !DEBUG
            DynamicMethod dm = new DynamicMethod("GetInterfaceCountDynamic", typeof(int), new Type[] {typeof(Type) }, typeof(SampleMethods).Module);
            ILGenerator ilg = dm.GetILGenerator(512);

            ilg.DeclareLocal(typeof(Type[]));
            
            for (int i = 0; i < instructions.Length; i++)
            {
                instructions[i].EmitTo(ilg);
            }
            var deleg = (Func<Type, int>)dm.CreateDelegate(typeof(Func<Type, int>));
            int res = deleg(typeof(List<>));
            Assert.AreEqual(SampleMethods.GetInterfaceCount(typeof(List<>)), res, "The result of GetInterfaceCountDynamic is wrong");
#endif
        }

        [TestMethod]
        public void Test_CilReader_GenericType()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintList");
            CilInstructionBase[] instructions = CilReader.GetInstructions(mi).ToArray();

            AssertThat.NotEmpty(instructions, "The result of PrintList method parsing should not be empty collection");
                        
            AssertThat.HasOnlyOneMatch(
                instructions,
                (x) => {
                    if (x.OpCode == OpCodes.Newobj)
                    {
                        Type t = ((MethodBase)x.ReferencedMember).DeclaringType;
                        if (!t.IsGenericType) return false;
                        if (t.Name != "List`1") return false;
                        if (t.GenericTypeArguments.Length == 0) return false;
                        return t.GenericTypeArguments[0] == typeof(string);
                    }
                    else return false;
                },
                "The result of PrintList method parsing should contain a single 'newobj' instruction creating List<string>"
                );

            AssertThat.HasAtLeastOneMatch(
                instructions,
                (x) => {
                    if (x.OpCode == OpCodes.Call || x.OpCode == OpCodes.Callvirt)
                    {
                        MethodInfo m = (MethodInfo)x.ReferencedMember;
                        if (m.Name != "Add") return false;

                        Type t = m.DeclaringType;
                        if (!t.IsGenericType) return false;
                        if (t.Name != "List`1") return false;
                        if (t.GenericTypeArguments.Length == 0) return false;
                        return t.GenericTypeArguments[0] == typeof(string);
                    }
                    else return false;
                },
                "The result of PrintList method parsing should contain at least one List<string>.Add call"
                );

            Assert.IsTrue(instructions[instructions.Length - 1].OpCode == OpCodes.Ret, "The last instruction of PrintList method should be 'ret'");

            //Test EmitTo: only NetFX            
#if !NETSTANDARD
            DynamicMethod dm = new DynamicMethod("PrintListDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module);
            ILGenerator ilg = dm.GetILGenerator(512);

            ilg.DeclareLocal(typeof(List<string>));
            
            for (int i = 0; i < instructions.Length; i++)
            {
                instructions[i].EmitTo(ilg);
            }
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();            
#endif
        }        

        [TestMethod]
        public void Test_CilReader_GenericParameter()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("GenerateArray");
            CilInstructionBase[] instructions = CilReader.GetInstructions(mi).ToArray();

            AssertThat.NotEmpty(instructions, "The result of GenerateArray method parsing should not be empty collection");            
               
            AssertThat.HasOnlyOneMatch(
                instructions,
                (x) => x.OpCode == OpCodes.Newarr && x.ReferencedType.IsGenericParameter == true && x.ReferencedType.GenericParameterPosition == 0,
                "The result of GenerateArray method parsing should contain a single 'newarr' instruction referencing generic parameter"
                );

            Assert.IsTrue(instructions[instructions.Length - 1].OpCode == OpCodes.Ret, "The last instruction of GenerateArray method should be 'ret'");
        }

        [TestMethod]
        public void Test_CilReader_ExternalAssemblyAccess()
        {
            MethodInfo mi = typeof(System.IO.Path).GetMethod("GetExtension");
            CilInstructionBase[] instructions = CilReader.GetInstructions(mi).ToArray();

            AssertThat.NotEmpty(instructions, "The result of Path.GetExtension method parsing should not be empty collection");
                  
            AssertThat.HasAtLeastOneMatch(
                instructions,
                (x) => x.OpCode == OpCodes.Ret,
                "The result of Path.GetExtension method parsing should contain at least one 'ret' instruction"
                );

            AssertThat.HasAtLeastOneMatch(
                instructions,
                (x) => x.OpCode != OpCodes.Nop && x.OpCode != OpCodes.Ret,
                "The result of Path.GetExtension method parsing should contain at least one instruction which is not 'nop' or 'ret'"
                );
        }        
    }
}
