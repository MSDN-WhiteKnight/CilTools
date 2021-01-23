/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class CilGraphTests
    {
        [TestMethod]
        public void Test_CilGraph_HelloWorld()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            CilGraphTestsCore.Test_CilGraph_HelloWorld(mi);

            //Test EmitTo: only NetFX
#if !NETSTANDARD
            CilGraph graph = CilGraph.Create(mi);
            DynamicMethod dm = new DynamicMethod(
                "CilGraphTests_PrintHelloWorldDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module
                );
            ILGenerator ilg = dm.GetILGenerator(512);
            graph.EmitTo(ilg);
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
#endif
        }

        [TestMethod]
        public void Test_CilGraph_Loop()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintTenNumbers");            
            CilGraphTestsCore.Test_CilGraph_Loop(mi);

            //Test EmitTo: only NetFX
#if !NETSTANDARD
            CilGraph graph = CilGraph.Create(mi);
            DynamicMethod dm = new DynamicMethod("CilGraphTests_PrintTenNumbersDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module);
            ILGenerator ilg = dm.GetILGenerator(512);
            graph.EmitTo(ilg);
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
#endif
        }

        [TestMethod]
        public void Test_CilGraph_Exceptions()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("DivideNumbers");
            CilGraphTestsCore.Test_CilGraph_Exceptions(mi);

            //Test EmitTo: only NetFX
#if !NETSTANDARD
            CilGraph graph = CilGraph.Create(mi);
            DynamicMethod dm = new DynamicMethod("CilGraphTests_DivideNumbersDynamic", typeof(bool),
                new Type[] { typeof(int), typeof(int), typeof(int).MakeByRefType() }, typeof(SampleMethods).Module);
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
#endif
        }

        [TestMethod]
        public void Test_CilGraph_GetHandlerNodes()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("DivideNumbers");
            CilGraphTestsCore.Test_CilGraph_GetHandlerNodes(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Tokens()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("TestTokens");
            CilGraphTestsCore.Test_CilGraph_Tokens(mi);

            //Test EmitTo: only NetFX
#if !NETSTANDARD
            CilGraph graph = CilGraph.Create(mi);
            DynamicMethod dm = new DynamicMethod(
                "CilGraphTests_TestTokensDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module
                );
            ILGenerator ilg = dm.GetILGenerator(512);
            graph.EmitTo(ilg);
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
#endif
        }

        [TestMethod]
        public void Test_CilGraph_Constrained()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("ConstrainedTest");
            CilGraphTestsCore.Test_CilGraph_Constrained(mi);
        }

#if DEBUG
        [TestMethod]
        public void Test_CilGraph_Pointer()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PointerTest");
            CilGraphTestsCore.Test_CilGraph_Pointer(mi);
        }
#endif

#if !NETSTANDARD
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

                    ilg.EmitCall(OpCodes.Call,miTarget,null);
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
        public void Test_CilGraph_DynamicMethod()
        {
            //create dynamic method
            DynamicMethod dm = new DynamicMethod(
                "DynamicMethodTest", typeof(int), new Type[] { typeof(string) }, typeof(SampleMethods).Module
                );
            ILGenerator ilg = dm.GetILGenerator(2048);
            ilg.DeclareLocal(typeof(string));

            ilg.BeginExceptionBlock();
            ilg.Emit(OpCodes.Ldstr, "Hello, world.");

            ilg.Emit(OpCodes.Stloc, (short)0);
            ilg.Emit(OpCodes.Ldloc, (short)0);
            ilg.Emit(
                OpCodes.Call,
                typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) })
                );

            ilg.Emit(OpCodes.Ldsfld, typeof(SampleMethods).GetField("f"));
            ilg.Emit(
                OpCodes.Call,
                typeof(Console).GetMethod("WriteLine", new Type[] { typeof(int) })
                );

            ilg.BeginCatchBlock(typeof(Exception));
            ilg.Emit(
                OpCodes.Call,
                typeof(Console).GetMethod("WriteLine", new Type[] { typeof(object) })
                );

            ilg.BeginFinallyBlock();

            ilg.Emit(OpCodes.Ldc_I4, 10);
            ilg.Emit(OpCodes.Newarr, typeof(Guid));
            ilg.Emit(OpCodes.Pop);
            ilg.EndExceptionBlock();

            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ret);

            var deleg = (Func<string, int>)dm.CreateDelegate(typeof(Func<string, int>));
            int res = deleg("Hello, world!");

            Console.WriteLine(Environment.Version.ToString());
            Diagnostics.Error += (x, y) => { Console.WriteLine(y.Exception.ToString()); };
            CilGraphTestsCore.Test_CilGraph_DynamicMethod(dm);
        }

        [TestMethod]
        public void Test_CilGraph_IndirectCall()
        {
            //create dynamic method
            DynamicMethod dm = new DynamicMethod("IndirectCallTest", typeof(void), new Type[] { typeof(string) }, typeof(SampleMethods).Module);
            ILGenerator ilg = dm.GetILGenerator(512);
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldftn, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));
            ilg.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), new Type[] { typeof(string) }, null);
            ilg.Emit(OpCodes.Ret);

            var deleg = (Action<string>)dm.CreateDelegate(typeof(Action<string>));
            deleg("Hello from System.Reflection.Emit!");

            //create CilGraph from DynamicMethod
            CilGraph graph = CilGraph.Create(dm);

            //verify CilGraph
            AssertThat.IsCorrect(graph);

            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            AssertThat.NotEmpty(nodes, "The result of IndirectCallTest method parsing should not be empty collection");

            AssertThat.HasOnlyOneMatch(
                nodes,(x) => x.Instruction.OpCode == OpCodes.Ldarg_0,"The result of IndirectCallTest method parsing should contain a single 'ldarg.0' instruction"
                );

            AssertThat.HasOnlyOneMatch(
                nodes,
                (x) => x.Instruction.OpCode == OpCodes.Ldftn && (x.Instruction.ReferencedMember as MethodInfo).Name == "WriteLine",
                "The result of IndirectCallTest method parsing should contain a single 'ldftn' instruction referencing Console.WriteLine"
                );

            AssertThat.HasOnlyOneMatch(
                nodes,
                (x) => x.Instruction.OpCode == OpCodes.Calli && 
                    x.Instruction.ReferencedSignature != null &&
                    x.Instruction.ReferencedSignature.ReturnType.Type.Name =="Void" &&
                    x.Instruction.ReferencedSignature.GetParamType(0).Type.Name == "String" &&
                    x.Instruction.ReferencedSignature.CallingConvention == CallingConvention.Default,
                "The result of IndirectCallTest method parsing should contain a single 'calli' instruction with signature matching Console.WriteLine"
                );

            AssertThat.HasOnlyOneMatch(
                nodes,(x) => x.Instruction.OpCode == OpCodes.Ret,
                "The result of IndirectCallTest method parsing should contain a single 'ret' instruction"
                );

            //Verify CilGraph.ToString() output
            string str = graph.ToText();

            AssertThat.IsMatch(str, new MatchElement[] { 
                new Literal(".method"), MatchElement.Any, new Literal("void"), MatchElement.Any, 
                new Literal("IndirectCallTest"), MatchElement.Any,
                new Literal("("),MatchElement.Any, new Literal("string"),MatchElement.Any, new Literal(")"), MatchElement.Any,
                new Literal("cil"), MatchElement.Any, new Literal("managed"), MatchElement.Any, 
                new Literal("{"), MatchElement.Any, 
                new Literal("ldarg.0"), MatchElement.Any, 
                new Literal("ldftn"), MatchElement.Any, new Literal("System.Console::WriteLine"), MatchElement.Any, 
                new Literal("calli"), MatchElement.Any, new Literal("void"), MatchElement.Any,
                 new Literal("("),MatchElement.Any, new Literal("string"),MatchElement.Any, new Literal(")"), MatchElement.Any,
                new Literal("ret"), MatchElement.Any, 
                new Literal("}")  
            });

        }
#endif
    }
}
