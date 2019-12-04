using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CilBytecodeParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilBytecodeParser.Tests
{
    [TestClass]
    public class CilGraphTests
    {
        [TestMethod]
        public void Test_CilGraph_HelloWorld()
        {
            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");
            CilGraph graph = CilAnalysis.GetGraph(mi);
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
            CilGraph graph = CilAnalysis.GetGraph(mi);
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

            //Test EmitTo: only NetFX
#if !NETSTANDARD
            DynamicMethod dm = new DynamicMethod("CilGraphTests_PrintTenNumbersDynamic", typeof(void), new Type[] { }, typeof(SampleMethods).Module);
            ILGenerator ilg = dm.GetILGenerator(512);
            graph.EmitTo(ilg);
            Action deleg = (Action)dm.CreateDelegate(typeof(Action));
            deleg();
#endif
        }
    }
}
