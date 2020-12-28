/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests.NetCore
{
    [TestClass]
    public class CilGraphTests
    {
        [TestMethod]
        public void Test_CilGraph_HelloWorld()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            CilGraphTestsCore.Test_CilGraph_HelloWorld(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Loop()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintTenNumbers");
            CilGraphTestsCore.Test_CilGraph_Loop(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Exceptions()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("DivideNumbers");
            CilGraphTestsCore.Test_CilGraph_Exceptions(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Tokens()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("TestTokens");
            CilGraphTestsCore.Test_CilGraph_Tokens(mi);
        }

#if DEBUG
        [TestMethod]
        public void Test_CilGraph_Pointer()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PointerTest");
            CilGraphTestsCore.Test_CilGraph_Pointer(mi);
        }
#endif

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

            //create CilGraph from DynamicMethod
            CilGraph graph = CilGraph.Create(dm);

            //verify CilGraph
            AssertThat.IsCorrect(graph);

            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            AssertThat.NotEmpty(nodes, "The result of DynamicMethodTest method parsing should not be empty collection");

            /*AssertThat.HasOnlyOneMatch(
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
                );*/

            //Verify CilGraph.ToString() output
    
            Diagnostics.OnError += (x,y)=>{Console.WriteLine(y.Exception.ToString());};
    
            string str = graph.ToText();
    
            Console.WriteLine(str);
            Console.WriteLine(Environment.Version.ToString());

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
                new Literal("call"), MatchElement.Any, new Literal("System.Console::WriteLine"), MatchElement.Any,
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
    }
}
