﻿/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.BytecodeAnalysis;
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
            CilGraph graph = CilGraph.Create(mi);
            AssertThat.IsCorrect(graph);

            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            AssertThat.NotEmpty(nodes, "The result of PrintHelloWorld method parsing should not be empty collection");
                        
            AssertThat.HasOnlyOneMatch(
                nodes,
                (x) => x.Instruction.OpCode == OpCodes.Ldstr && x.Instruction.ReferencedString == "Hello, World",
                "The result of PrintHelloWorld method parsing should contain a single 'ldstr' instruction referencing \"Hello, World\" literal"
                );

            //verify that node sequence contains a single call to Console.WriteLine
            AssertThat.HasOnlyOneMatch(
                nodes,
                (x) =>
                {
                    if (x.Instruction.OpCode != OpCodes.Call) return false;
                    var method = x.Instruction.ReferencedMember as MethodBase;
                    if (method == null) return false;

                    return method.Name == "WriteLine" && method.DeclaringType.Name == "Console";
                },
                "The result of PrintHelloWorld method parsing should contain a single call to Console.WriteLine"
                );

            CilGraphNode last = nodes[nodes.Length - 1];                       
            Assert.IsTrue(last.Instruction.OpCode == OpCodes.Ret, "The last instruction of PrintHelloWorld method should be 'ret'");
            Assert.IsNull(last.BranchTarget, "The 'ret' instruction should not have branch target");

            //Test conversion to string
            string str = graph.ToString();
            AssertThat.IsMatch(str, new MatchElement[] { new Literal(".method"), MatchElement.Any, new Literal("public") });
            AssertThat.IsMatch(str, new MatchElement[] { new Literal(".method"), MatchElement.Any, new Literal("static") });

            AssertThat.IsMatch(str, new MatchElement[] { 
                new Literal(".method"), MatchElement.Any, new Literal("void"), MatchElement.Any, 
                new Literal("PrintHelloWorld"), MatchElement.Any, 
                new Literal("cil"), MatchElement.Any, new Literal("managed"), MatchElement.Any, 
                new Literal("{"), MatchElement.Any, 
                new Literal("ldstr"), MatchElement.Any, new Literal("\"Hello, World\""), MatchElement.Any,
                new Literal("call"), MatchElement.Any, 
                new Literal("void"), MatchElement.Any, new Literal("System.Console::WriteLine"), MatchElement.Any,
                new Literal("ret"), MatchElement.Any, 
                new Literal("}") 
            });
            

            //Test EmitTo: only NetFX
#if !NETSTANDARD
            DynamicMethod dm = new DynamicMethod("CilGraphTests_PrintHelloWorldDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module);
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
            CilGraph graph = CilGraph.Create(mi);
            AssertThat.IsCorrect(graph);

            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            AssertThat.NotEmpty(nodes, "The result of PrintTenNumbers method parsing should not be empty collection");

            AssertThat.HasAtLeastOneMatch(
                nodes,
                (x) => x.BranchTarget != null,
                "The 'for' loop parsing results should contain at least one node with branch target"
                );

            CilGraphNode last = nodes[nodes.Length - 1];
            Assert.IsTrue(last.Instruction.OpCode == OpCodes.Ret, "The last instruction of PrintTenNumbers method should be 'ret'");
            Assert.IsNull(last.BranchTarget, "The 'ret' instruction should not have branch target");

            //Test conversion to string
            string str = graph.ToString();
            AssertThat.IsMatch(str, new MatchElement[] { new Literal(".method"), MatchElement.Any, new Literal("public") });
            AssertThat.IsMatch(str, new MatchElement[] { new Literal(".method"), MatchElement.Any, new Literal("static") });

            AssertThat.IsMatch(str, new MatchElement[] { 
                new Literal(".method"), MatchElement.Any, new Literal("void"), MatchElement.Any, 
                new Literal("PrintTenNumbers"), MatchElement.Any, 
                new Literal("cil"), MatchElement.Any, new Literal("managed"), MatchElement.Any, 
                new Literal("{"), MatchElement.Any, 
                new Literal(".locals"), MatchElement.Any, new Literal("int32"), MatchElement.Any, 
                new Literal("call"), MatchElement.Any, 
                new Literal("void"), MatchElement.Any, new Literal("System.Console::WriteLine"), MatchElement.Any,
                new Literal("ret"), MatchElement.Any, 
                new Literal("}") 
            });

            AssertThat.IsMatch(str, new MatchElement[] { new Literal("IL_"), MatchElement.Any, new Literal(":") });

            //Test EmitTo: only NetFX
#if !NETSTANDARD
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
            CilGraph graph = CilGraph.Create(mi);
            AssertThat.IsCorrect(graph);

            //Test conversion to string
            string str = graph.ToString();
            AssertThat.IsMatch(str, new MatchElement[] { new Literal(".method"), MatchElement.Any, new Literal("public") });
            AssertThat.IsMatch(str, new MatchElement[] { new Literal(".method"), MatchElement.Any, new Literal("static") });

            AssertThat.IsMatch(str, new MatchElement[] { 
                new Literal(".method"), MatchElement.Any, new Literal("bool"), MatchElement.Any, 
                new Literal("DivideNumbers"), MatchElement.Any, 
                new Literal("cil"), MatchElement.Any, new Literal("managed"), MatchElement.Any, 
                new Literal("{"), MatchElement.Any, 
                new Literal(".try"), MatchElement.Any, new Literal("{"), MatchElement.Any, 
                new Literal("}"), MatchElement.Any, 
                new Literal("catch"), MatchElement.Any, new Literal("DivideByZeroException"), MatchElement.Any,
                new Literal("{"), MatchElement.Any,
                new Literal("}"), MatchElement.Any, 
                new Literal("finally"), MatchElement.Any, 
                new Literal("{"), MatchElement.Any,
                new Literal("endfinally"), MatchElement.Any, 
                new Literal("}"), MatchElement.Any, 
                new Literal("ret"), MatchElement.Any, 
                new Literal("}") 
            });

            //Test EmitTo: only NetFX
#if !NETSTANDARD
            DynamicMethod dm = new DynamicMethod("CilGraphTests_DivideNumbersDynamic", typeof(bool), 
                new Type[] { typeof(int),typeof(int),typeof(int).MakeByRefType() }, typeof(SampleMethods).Module);
            ILGenerator ilg = dm.GetILGenerator(512);
            graph.EmitTo(ilg);
            SampleMethods.DivideNumbersDelegate deleg = (SampleMethods.DivideNumbersDelegate)dm.CreateDelegate(typeof(SampleMethods.DivideNumbersDelegate));

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
        public void Test_CilGraph_Tokens()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("TestTokens");
            CilGraph graph = CilGraph.Create(mi);
            AssertThat.IsCorrect(graph);

            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            AssertThat.NotEmpty(nodes, "The result of TestTokens method parsing should not be empty collection");

            AssertThat.HasOnlyOneMatch(
                nodes,
                (x) => x.Instruction.OpCode == OpCodes.Ldtoken && x.Instruction.ReferencedType.FullName.Contains("System.Int32"),
                "The result of TestTokens method parsing should contain a single 'ldtoken' instruction referencing System.Int32"
                );

            CilGraphNode last = nodes[nodes.Length - 1];
            Assert.IsTrue(last.Instruction.OpCode == OpCodes.Ret, "The last instruction of TestTokens method should be 'ret'");

            //Test conversion to string
            string str = graph.ToString();
            AssertThat.IsMatch(str, new MatchElement[] { new Literal("ldtoken"), MatchElement.Any, new Literal("System.Int32") });
            
            //Test EmitTo: only NetFX
#if !NETSTANDARD
            DynamicMethod dm = new DynamicMethod("CilGraphTests_TestTokensDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module);
            ILGenerator ilg = dm.GetILGenerator(512);
            graph.EmitTo(ilg);
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
#endif
        }

#if !NETSTANDARD
        [TestMethod]
        public void Test_CilGraph_Emit() //Test EmitTo: only NetFX
        {
            DynamicMethod dm = new DynamicMethod("CilGraphTests_EmitTest", typeof(void), new Type[] { }, typeof(SampleMethods).Module);
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
            DynamicMethod dm = new DynamicMethod("DynamicMethodTest", typeof(int), new Type[] { typeof(string) }, typeof(SampleMethods).Module);
            ILGenerator ilg = dm.GetILGenerator(512);
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
            ilg.Emit(OpCodes.Ret);

            ilg.BeginCatchBlock(typeof(Exception));
            ilg.Emit(
                OpCodes.Call,
                typeof(Console).GetMethod("WriteLine", new Type[] { typeof(object) })
                );
            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ret);
            ilg.BeginFinallyBlock();

            ilg.Emit(OpCodes.Ldc_I4, 10);
            ilg.Emit(OpCodes.Newarr, typeof(Guid));
            ilg.Emit(OpCodes.Pop);
            ilg.EndExceptionBlock();

            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ret);

            var deleg = (Func<string, int>)dm.CreateDelegate(typeof(Func<string, int>));                        
            int res = deleg("Hello, world!");

            //create CilGraph from DynamicMethod
            CilGraph graph = CilGraph.Create(dm);

            //verify CilGraph
            AssertThat.IsCorrect(graph);

            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            AssertThat.NotEmpty(nodes, "The result of DynamicMethodTest method parsing should not be empty collection");

            AssertThat.HasOnlyOneMatch(
                nodes,
                (x) => x.Instruction.OpCode == OpCodes.Ldstr && x.Instruction.ReferencedString == "Hello, world.",
                "The result of DynamicMethodTest method parsing should contain a single 'ldstr' instruction referencing \"Hello, world.\" literal"
                );

            AssertThat.HasOnlyOneMatch(
                nodes,
                (x) =>
                {
                    if (x.Instruction.OpCode != OpCodes.Call) return false;

                    var method = x.Instruction.ReferencedMember as MethodBase;

                    if (method == null) return false;

                    if (method.Name != "WriteLine") return false;

                    ParameterInfo[] pars = method.GetParameters();

                    if (pars.Length != 1) return false;

                    return pars[0].ParameterType.Name == "String";
                },
                "The result of DynamicMethodTest method parsing should contain a single call to Console.WriteLine(string)"
                );

            AssertThat.HasOnlyOneMatch(
                nodes,
                (x) => x.Instruction.OpCode == OpCodes.Ldsfld && (x.Instruction.ReferencedMember as FieldInfo).Name == "f",
                "The result of DynamicMethodTest method parsing should contain a single 'ldsfld' instruction referencing 'f'"
                );

            AssertThat.HasOnlyOneMatch(
                nodes,
                (x) => x.Instruction.OpCode == OpCodes.Newarr && x.Instruction.ReferencedType.Name == "Guid",
                "The result of DynamicMethodTest method parsing should contain a single 'newarr' instruction referencing Guid type"
                );
            
            //Verify CilGraph.ToString() output
            string str = graph.ToString();

            AssertThat.IsMatch(str, new MatchElement[] { 
                new Literal(".method"), MatchElement.Any, new Literal("int32"), MatchElement.Any, 
                new Literal("DynamicMethodTest"), MatchElement.Any,
                new Literal("("),MatchElement.Any, new Literal("string"),MatchElement.Any, new Literal(")"), MatchElement.Any,
                new Literal("cil"), MatchElement.Any, new Literal("managed"), MatchElement.Any, 
                new Literal("{"), MatchElement.Any, 
                new Literal(".locals"), MatchElement.Any, 
                new Literal("("), MatchElement.Any, new Literal("string"), MatchElement.Any, new Literal(")"), MatchElement.Any,
                new Literal(".try"), MatchElement.Any, new Literal("{"), MatchElement.Any, 
                new Literal("ldstr"), MatchElement.Any, new Literal("\"Hello, world.\""), MatchElement.Any,
                new Literal("call"), MatchElement.Any, new Literal("System.Console::WriteLine"), MatchElement.Any,
                new Literal("ldsfld"), MatchElement.Any, new Literal("f"), MatchElement.Any,
                new Literal("ret"), MatchElement.Any,
                new Literal("leave"), MatchElement.Any,
                new Literal("}"), MatchElement.Any, 
                new Literal("catch"), MatchElement.Any, new Literal("Exception"), MatchElement.Any,
                new Literal("{"), MatchElement.Any, 
                new Literal("}"), MatchElement.Any, 
                new Literal("finally"), MatchElement.Any, new Literal("{"), MatchElement.Any,
                new Literal("ldc.i4"), MatchElement.Any,new Literal("10"), MatchElement.Any,
                new Literal("newarr"), MatchElement.Any,new Literal("System.Guid"), MatchElement.Any,
                new Literal("pop"), MatchElement.Any,
                new Literal("endfinally"), MatchElement.Any, 
                new Literal("}"), MatchElement.Any, 
                new Literal("ldc.i4.0"), MatchElement.Any, 
                new Literal("ret"), MatchElement.Any, 
                new Literal("}")  
            });
            
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
            string str = graph.ToString();

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
