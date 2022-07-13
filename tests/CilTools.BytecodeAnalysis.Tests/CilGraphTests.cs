/* CilTools.BytecodeAnalysis library tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilTools.Syntax;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.BytecodeAnalysis.Tests
{
    [TestClass]
    public class CilGraphTests
    {
        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintHelloWorld", BytecodeProviders.All)]
        public void Test_CilGraph_HelloWorld(MethodBase mi)
        {
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
            string str = graph.ToText();
            CilGraphTestsCore.Assert_CilGraph_HelloWorld(str);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "PrintTenNumbers", BytecodeProviders.All)]
        public void Test_CilGraph_Loop(MethodBase mi)
        {
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
            string str = graph.ToText();
            CilGraphTestsCore.Assert_CilGraph_Loop(str);
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "DivideNumbers", BytecodeProviders.All)]
        public void Test_CilGraph_Exceptions(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            AssertThat.IsCorrect(graph);

            //Test conversion to string
            string str = graph.ToText();
            AssertThat.IsMatch(str, new Text[] { ".method", Text.Any, "public" });
            AssertThat.IsMatch(str, new Text[] { ".method", Text.Any, "static" });

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any, "bool", Text.Any,
                "DivideNumbers", Text.Any,
                "cil", Text.Any, "managed", Text.Any,
                "{", Text.Any,
                ".try", Text.Any, "{", Text.Any,
                "}", Text.Any,
                "catch", Text.Any, "DivideByZeroException", Text.Any,
                "{", Text.Any,
                "}", Text.Any,
                "finally", Text.Any,
                "{", Text.Any,
                "endfinally", Text.Any,
                "}", Text.Any,
                "ret", Text.Any,
                "}"
            });
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "DivideNumbers", BytecodeProviders.All)]
        public void Test_CilGraph_GetHandlerNodes(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            AssertThat.IsCorrect(graph);
            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].Instruction.OpCode == OpCodes.Div)
                {
                    ExceptionBlock[] blocks = nodes[i].GetExceptionBlocks();

                    ExceptionBlock block = blocks.
                        Where((x) => x.Flags.HasFlag(ExceptionHandlingClauseOptions.Finally)).
                        First();

                    CilGraphNode[] handler = graph.GetHandlerNodes(block).ToArray();
                    int last = handler.Length - 1;

                    Assert.IsTrue(String.Equals(
                        "endfinally", handler[last].Instruction.Name, StringComparison.InvariantCulture
                        ));

                    return;
                }
            }

            Assert.Fail("div instruction not found");
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "TestTokens", BytecodeProviders.All)]
        public void Test_CilGraph_Tokens(MethodBase mi)
        {
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
            string str = graph.ToText();
            AssertThat.IsMatch(str, new Text[] { "ldtoken", Text.Any, "System.Int32" });
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "ConstrainedTest", BytecodeProviders.All)]
        public void Test_CilGraph_Constrained(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            AssertThat.IsCorrect(graph);

            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            AssertThat.NotEmpty(nodes, "The result of ConstrainedTest method parsing should not be empty collection");

            AssertThat.HasOnlyOneMatch(
                nodes,
                (x) =>
                {
                    return x.Instruction.OpCode == OpCodes.Constrained &&
                           (x.Instruction.ReferencedMember as Type).IsGenericParameter;
                });

            //text
            string str = graph.ToText();

            AssertThat.IsMatch(str, new Text[] {
                Text.Any, "ConstrainedTest",
                Text.Any, "constrained.", Text.Any,
                "!!", Text.Any,
                "callvirt", Text.Any,
                "ToString", Text.Any,
            });

            /*
            .method  public hidebysig static void ConstrainedTest<T>(
                !!0 x
            ) cil managed
            {
             .maxstack  8

                      nop          
                      ldarga.s     x
                      constrained. !!T
                      callvirt     instance string [mscorlib]System.Object::ToString()
                      call         void [mscorlib]System.Console::WriteLine(string)
                      nop          
                      ret          
            } 
            */

            //syntax
            MethodDefSyntax mds = graph.ToSyntaxTree();
            AssertThat.IsSyntaxTreeCorrect(mds);
            SyntaxNode[] chilren = mds.Body.GetChildNodes();

            AssertThat.HasOnlyOneMatch(
                chilren,
                (x) =>
                {
                    return x is InstructionSyntax && ((InstructionSyntax)x).Operation == "constrained." &&
                    ((InstructionSyntax)x).OperandString.Contains("!!");
                });
        }

        [ConditionalTest(TestCondition.WindowsOnly, "Dynamic methods are not supported on non-Windows platforms")]
        [WorkItem(49)]
        public void Test_CilGraph_DynamicMethod()
        {
            //Skipped on Linux (https://github.com/MSDN-WhiteKnight/CilTools/issues/49)

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
            string str = graph.ToText();
            Console.WriteLine(str);

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any, "int32", Text.Any,
                "DynamicMethodTest", Text.Any,
                "(",Text.Any, "string",Text.Any, ")", Text.Any,
                "cil", Text.Any, "managed", Text.Any,
                "{", Text.Any,
                ".locals", Text.Any,
                "(", Text.Any, "string", Text.Any, ")", Text.Any,
                ".try", Text.Any, "{", Text.Any,
                "ldstr", Text.Any, "\"Hello, world.\"", Text.Any,
                "call", Text.Any, "System.Console::WriteLine", Text.Any,
                "ldsfld", Text.Any, "f", Text.Any,
                "call", Text.Any, "System.Console::WriteLine", Text.Any,
                "leave", Text.Any,
                "}", Text.Any,
                "catch", Text.Any, "Exception", Text.Any,
                "{", Text.Any,
                "}", Text.Any,
                "finally", Text.Any, "{", Text.Any,
                "ldc.i4", Text.Any,"10", Text.Any,
                "newarr", Text.Any,"System.Guid", Text.Any,
                "pop", Text.Any,
                "endfinally", Text.Any,
                "}", Text.Any,
                "ldc.i4.0", Text.Any,
                "ret", Text.Any,
                "}"
            });
        }

        [TestMethod]
        [MethodTestData(typeof(SampleMethods), "FilteredExceptionsTest", BytecodeProviders.All)]
        public void Test_CilGraph_ExceptionFilters(MethodBase m)
        {
            CilGraph graph = CilGraph.Create(m);
            AssertThat.IsCorrect(graph);
            
            CilGraphNode[] nodes = graph.GetNodes().ToArray();
            AssertThat.NotEmpty(nodes);

            //find node inside try block
            CilGraphNode node = nodes.
                Where((x) => x.Instruction.OpCode == OpCodes.Call && x.Instruction.ReferencedMember.Name == "ReadAllLines").
                Single();

            //find filter block
            ExceptionBlock[] blocks=node.GetExceptionBlocks();
            Assert.AreEqual(1, blocks.Length);
            ExceptionBlock block=blocks[0];
            Assert.AreEqual(ExceptionHandlingClauseOptions.Filter, block.Flags);

            //verify contents of filter block
            AssertThat.HasOnlyOneMatch(nodes, (x) =>
            {
                return x.Instruction.OpCode == OpCodes.Callvirt && 
                x.Instruction.ReferencedMember.Name == "get_Message" &&
                x.Instruction.ByteOffset >= block.FilterOffset &&
                x.Instruction.ByteOffset <= block.HandlerOffset;
            });

            AssertThat.HasOnlyOneMatch(nodes, (x) =>
            {
                return x.Instruction.OpCode == OpCodes.Callvirt &&
                x.Instruction.ReferencedMember.Name == "Contains" &&
                x.Instruction.ByteOffset >= block.FilterOffset &&
                x.Instruction.ByteOffset <= block.HandlerOffset;
            });

            //verify disassembler output
            string str = graph.ToText();

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any, "static", Text.Any,
                "string", Text.Any,
                "FilteredExceptionsTest", Text.Any,
                "(",Text.Any, ")", Text.Any,
                "cil", Text.Any, "managed", Text.Any,
                "{", Text.Any,
                
                ".try", Text.Any, "{", Text.Any,                
                "call", Text.Any, "System.IO.File::ReadAllLines", Text.Any,
                "}", Text.Any,

                "filter", Text.Any, "{", Text.Any,
                "callvirt", Text.Any, "get_Message", Text.Any,
                "callvirt", Text.Any, "Contains", Text.Any,
                "endfilter", Text.Any,"}", Text.Any,

                //handler
                "{", Text.Any,
                "ldsfld", Text.Any, "System.String::Empty", Text.Any,
                "}", Text.Any,

                "ret", Text.Any,
                "}"
            });
        }

        [ConditionalTest(TestCondition.DebugBuildOnly, "Codegen is different in release build")]
        [MethodTestData(typeof(SampleMethods), "PointerTest", BytecodeProviders.All)]
        public void Test_CilGraph_Pointer(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            AssertThat.IsCorrect(graph);

            CilGraphNode[] nodes = graph.GetNodes().ToArray();

            AssertThat.NotEmpty(nodes, "The result of PointerTest method parsing should not be empty collection");

            AssertThat.HasOnlyOneMatch(
                nodes,
                (x) => x.Instruction.OpCode == OpCodes.Ldsflda && x.Instruction.ReferencedMember.Name == "f",
                "The result of PointerTest method parsing should contain a single 'ldsflda' instruction referencing field 'f'"
                );

            //text
            string str = graph.ToText();
            Assert.IsTrue(str.Contains("int32*"), "PointerTest disassembled code should contain 'int32*'");

            AssertThat.IsMatch(str, new Text[] {
                Text.Any, "ldsflda", Text.Any,
                "SampleMethods", Text.Any,"::", Text.Any,
                "f", Text.Any
            });

            //syntax
            MethodDefSyntax mds = graph.ToSyntaxTree();
            AssertThat.IsSyntaxTreeCorrect(mds);
            SyntaxNode[] chilren = mds.Body.GetChildNodes();

            AssertThat.HasOnlyOneMatch(
                chilren,
                (x) => x is InstructionSyntax && ((InstructionSyntax)x).Operation == "ldsflda" &&
                ((InstructionSyntax)x).OperandString.Contains("f"),
                "PointerTest syntax tree should contain a single 'ldsflda' instruction referencing field 'f'"
                );

            AssertThat.HasAtLeastOneMatch(
                chilren,
                (x) => x is DirectiveSyntax && ((DirectiveSyntax)x).Name == "locals" &&
                ((DirectiveSyntax)x).ContentString.Contains("int32*"),
                "PointerTest syntax tree should contain a single '.locals' directive with 'int32*' variable"
                );
        }
    }
}
