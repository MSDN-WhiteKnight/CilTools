/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class EmitTests
    {
        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintTenNumbers", BytecodeProviders.Reflection)]
        public void Test_CilGraph_Loop_Emit(MethodBase mi)
        {
            //Test EmitTo: only NetFX

            CilGraph graph = CilGraph.Create(mi);
            DynamicMethod dm = new DynamicMethod(
                "CilGraphTests_PrintTenNumbersDynamic", typeof(void), new Type[] { },
                typeof(SampleMethods).Module);

            ILGenerator ilg = dm.GetILGenerator(512);
            graph.EmitTo(ilg);
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.Reflection)]
        public void Test_CilGraph_HelloWorld_Emit(MethodBase mi)
        {
            //Test EmitTo: only NetFX

            CilGraph graph = CilGraph.Create(mi);
            DynamicMethod dm = new DynamicMethod(
                "CilGraphTests_PrintHelloWorldDynamic", typeof(void), new Type[] { },
                typeof(SampleMethods).Module
                );
            ILGenerator ilg = dm.GetILGenerator(512);
            graph.EmitTo(ilg);
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "DivideNumbers", BytecodeProviders.Reflection)]
        public void Test_CilGraph_Exceptions_Emit(MethodBase mi)
        {
            //Test EmitTo: only NetFX

            CilGraph graph = CilGraph.Create(mi);
            DynamicMethod dm = new DynamicMethod(
                "CilGraphTests_DivideNumbersDynamic", typeof(bool),
                new Type[] { typeof(int), typeof(int), typeof(int).MakeByRefType() },
                typeof(SampleMethods).Module);

            ILGenerator ilg = dm.GetILGenerator(512);
            graph.EmitTo(ilg);
            SampleMethods.DivideNumbersDelegate deleg = (SampleMethods.DivideNumbersDelegate)dm.CreateDelegate(
                typeof(SampleMethods.DivideNumbersDelegate)
                );

            int res;
            bool success;

            success = deleg(4, 2, out res);
            Assert.IsTrue(success, "The calculation of 4/2 should not fail");
            Assert.AreEqual(2, res, "The result of 4/2 should be 2");

            success = deleg(1, 0, out res);
            Assert.IsFalse(success, "The calculation of 1/0 should fail");
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TestTokens", BytecodeProviders.Reflection)]
        public void Test_CilGraph_Tokens_Emit(MethodBase mi)
        {
            //Test EmitTo: only NetFX

            CilGraph graph = CilGraph.Create(mi);
            DynamicMethod dm = new DynamicMethod(
                "CilGraphTests_TestTokensDynamic", typeof(void), new Type[] { },
                typeof(SampleMethods).Module);

            ILGenerator ilg = dm.GetILGenerator(512);
            graph.EmitTo(ilg);
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
        }

        [TestMethod]
        public void Test_CilGraph_Emit() //Test EmitTo: only NetFX
        {
            DynamicMethod dm = new DynamicMethod(
                "CilGraphTests_EmitTest", typeof(void), new Type[] { }, typeof(SampleMethods).Module
                );
            ILGenerator ilg = dm.GetILGenerator(512);
            MethodInfo miTemplate = typeof(SampleMethods).GetMethod("TemplateMethod");
            CilGraph graph = CilGraph.Create(miTemplate);
            MethodInfo miTarget = typeof(SampleMethods).GetMethod("IncrementCounter");

            graph.EmitTo(ilg, (instr) =>
            {
                if ((instr.OpCode.Equals(OpCodes.Call) || instr.OpCode.Equals(OpCodes.Callvirt)) &&
                    instr.ReferencedMember.Name == "DoSomething")
                {
                    //replace every DoSomething call by IncrementCounter call

                    ilg.EmitCall(OpCodes.Call, miTarget, null);
                    return true; //handled
                }
                else return false;
            });

            //create and execute delegate
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            SampleMethods.counter = 0;
            deleg();
            Assert.AreEqual<int>(15, SampleMethods.counter);
        }

        [TestMethod]
        public void Test_CilGraph_Switch()
        {
            DynamicMethod dm = new DynamicMethod(
                "CilGraphTests_SwitchTestDynamic", typeof(string), new Type[] { typeof(int) }, typeof(SampleMethods).Module
                );
            ILGenerator ilg = dm.GetILGenerator(512);
            MethodInfo mi = typeof(SampleMethods).GetMethod("SwitchTest");
            CilGraph graph = CilGraph.Create(mi);

            graph.EmitTo(ilg);

            //create and execute delegate
            Func<int, string> deleg = (Func<int, string>)dm.CreateDelegate(typeof(Func<int, string>));
            string res = deleg(1);
            Assert.AreEqual("One", res);
            res = deleg(2);
            Assert.AreEqual("Two", res);
            res = deleg(120);
            Assert.AreEqual((120).ToString(), res);
        }

        [TestMethod]
        public void Test_CilGraph_IndirectCall()
        {
            //create dynamic method
            DynamicMethod dm = new DynamicMethod(
                "IndirectCallTest", typeof(void), new Type[] { typeof(string) }, typeof(SampleMethods).Module
            );
            
            ILGenerator ilg = dm.GetILGenerator(512);
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldftn, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));
            ilg.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), new Type[] { typeof(string) }, null);
            ilg.Emit(OpCodes.Ret);

            //verify that dynamic method executes and does not throw
            var deleg = (Action<string>)dm.CreateDelegate(typeof(Action<string>));
            deleg("Hello from System.Reflection.Emit!");

            //main test logic
            IlAsmTests.IndirectCall_VerifyMethod(dm);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.Reflection)]
        public void Test_CilReader_HelloWorld_Emit(MethodBase mi)
        {
            //Test EmitTo: only NetFX

            CilInstruction[] instructions = CilReader.GetInstructions(mi).ToArray();
            DynamicMethod dm = new DynamicMethod(
                "PrintHelloWorldDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module
                );
            ILGenerator ilg = dm.GetILGenerator(512);
            for (int i = 0; i < instructions.Length; i++)
            {
                instructions[i].EmitTo(ilg);
            }
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "SquareFoo", BytecodeProviders.Reflection)]
        public void Test_CilReader_StaticFieldAccess_Emit(MethodBase mi)
        {
            //Test EmitTo: only NetFX

            CilInstruction[] instructions = CilReader.GetInstructions(mi).ToArray();
            DynamicMethod dm = new DynamicMethod("SquareFooDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module);
            ILGenerator ilg = dm.GetILGenerator(512);
            for (int i = 0; i < instructions.Length; i++)
            {
                instructions[i].EmitTo(ilg);
            }
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
            Assert.AreEqual(4, SampleMethods.Foo, "The value of SampleMethods.Foo is wrong");
        }

#if !DEBUG
        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "GetInterfaceCount", MethodSource.FromReflection)]
        public void Test_CilReader_VirtualCall_Emit(MethodBase mi)
        {
            //Test EmitTo: only NetFX
            //Dont' run in debug, because compiler generates branching here for some reason

            CilInstruction[] instructions = CilReader.GetInstructions(mi).ToArray();
            DynamicMethod dm = new DynamicMethod(
                "GetInterfaceCountDynamic", typeof(int), new Type[] {typeof(Type) }, typeof(SampleMethods).Module
            );
            ILGenerator ilg = dm.GetILGenerator(512);

            ilg.DeclareLocal(typeof(Type[]));
            
            for (int i = 0; i < instructions.Length; i++)
            {
                instructions[i].EmitTo(ilg);
            }
            var deleg = (Func<Type, int>)dm.CreateDelegate(typeof(Func<Type, int>));
            int res = deleg(typeof(List<>));
            Assert.AreEqual(SampleMethods.GetInterfaceCount(typeof(List<>)), res, "The result of GetInterfaceCountDynamic is wrong");
        }
#endif

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintList", BytecodeProviders.Reflection)]
        public void Test_CilReader_GenericType_Emit(MethodBase mi)
        {
            //Test EmitTo: only NetFX            

            CilInstruction[] instructions = CilReader.GetInstructions(mi).ToArray();
            DynamicMethod dm = new DynamicMethod(
                "PrintListDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module
                );
            ILGenerator ilg = dm.GetILGenerator(512);

            ilg.DeclareLocal(typeof(List<string>));

            for (int i = 0; i < instructions.Length; i++)
            {
                instructions[i].EmitTo(ilg);
            }
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
        }
    }
}
