﻿/* CIL Tools
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CilTools.BytecodeAnalysis;
using CilTools.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Tests.Common
{
    public class CilGraphTestsCore
    {
        public static void Test_CilGraph_HelloWorld(MethodBase mi)
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
        }

        public static void Test_CilGraph_Loop(MethodBase mi)
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
        }

        public static void Test_CilGraph_Exceptions(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            AssertThat.IsCorrect(graph);

            //Test conversion to string
            string str = graph.ToText();
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
        }

        public static void Test_CilGraph_Tokens(MethodBase mi)
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
            AssertThat.IsMatch(str, new MatchElement[] { new Literal("ldtoken"), MatchElement.Any, new Literal("System.Int32") });
        }

        public static void Test_CilGraph_Pointer(MethodBase mi)
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
            
            AssertThat.IsMatch(str, new MatchElement[] { 
                MatchElement.Any, new Literal("ldsflda"), MatchElement.Any,
                new Literal("SampleMethods"), MatchElement.Any,new Literal("::"), MatchElement.Any,
                new Literal("f"), MatchElement.Any
            });

            //syntax
            MethodDefSyntax mds = graph.ToSyntaxTree();
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
