# Using CilTools.BytecodeAnalysis

CilTools.BytecodeAnalysis library reads .NET methods' Common Intermediate Language (CIL) bytecode and converts it into high-level objects or textual CIL representation so they can be easily studied and programmatically processed. The input data for the bytecode analysis is either the raw byte array with bytecode, or the reflection `MethodBase` object. When the bytecode source is the raw byte array, metadata tokens resolution is not supported; you can only read opcodes and raw operands. For `MethodBase` objects, you can use both the standard reflection implementation or custom implementations, as long as they support fetching the method body.

Classes are declared in the <xref:CilTools.BytecodeAnalysis> namespace. The <xref:CilTools.BytecodeAnalysis.Extensions> namespace provides an alternative syntax via extension methods for reflection classes.

## Prerequisites

To use CilTools.BytecodeAnalysis, install [CilTools.BytecodeAnalysis](https://www.nuget.org/packages/CilTools.BytecodeAnalysis/) NuGet package. You can also build the library from sources or download released binaries in [CIL Tools repository](https://github.com/MSDN-WhiteKnight/CilTools). The minimum target framework is .NET Framework 3.5 or .NET Standard 2.0.

## Working with instructions

To get the collection of instructions that make up the method body, use <xref:CilTools.BytecodeAnalysis.CilReader.GetInstructions(System.Reflection.MethodBase)>. You can also use extension method `GetInstructions` for `MethodBase` class. To print the CIL assembler representation of the instruction, call its `ToString` method. The following example shows how to display all instructions in the method.

```
using System;
using System.Collections.Generic;
using CilTools.BytecodeAnalysis;
using CilTools.BytecodeAnalysis.Extensions;

class Program
{
    public static void Hello()
    {
        int a = 1;
        int b = 2;
        Console.WriteLine("Hello, World");
        Console.WriteLine("{0} + {1} = {2}",a,b,a+b);
    }

    static void Main(string[] args)
    {
        IEnumerable<CilInstruction> instructions = typeof(Program).GetMethod("Hello").GetInstructions();

        foreach (CilInstruction instr in instructions)
        {
            Console.WriteLine(instr.ToString());
        }
        Console.ReadKey();
    }

}


/* Output:

nop
ldc.i4.1
stloc.0
ldc.i4.2
stloc.1
ldstr      "Hello, World"
call       void [mscorlib]System.Console::WriteLine(string)
nop
ldstr      "{0} + {1} = {2}"
ldloc.0
box        [mscorlib]System.Int32
ldloc.1
box        [mscorlib]System.Int32
ldloc.0
ldloc.1
add
box        [mscorlib]System.Int32
call       void [mscorlib]System.Console::WriteLine(string, object, object, object)
nop
ret
*/
```

You can also access individual instruction properties, such as <xref:CilTools.BytecodeAnalysis.CilInstruction.OpCode> or <xref:CilTools.BytecodeAnalysis.CilInstruction.Operand>, to figure out what the instruction does. If the instruction references other member, for example, calls a method, you can use the <xref:CilTools.BytecodeAnalysis.CilInstruction.ReferencedMember> property to get the reflection object for that member.

## Working with CIL graph

Use the <xref:CilTools.BytecodeAnalysis.CilGraph.Create(System.Reflection.MethodBase)> method to get a a graph that represents a flow of control between method's instructions. Working with CIL graph enables you to process branching or exception blocks, which is not possible when working with individual instructions. CIL graph consists of nodes that represents instuctions, with edges representing control flow between them. Use <xref:CilTools.BytecodeAnalysis.CilGraph.GetNodes> method to get the collection of nodes. You can also disassemble method using the `ToText` method on `CilGraph` class, or using the <xref:CilTools.BytecodeAnalysis.CilAnalysis.MethodToText(System.Reflection.MethodBase)> static helper, when you need to output method's CIL code as text.

The following example shows how to construct the CIL graph for the specified method and print its disassembled CIL:

```
using System;
using CilTools.BytecodeAnalysis;
using CilTools.BytecodeAnalysis.Extensions;

class Program
{
    public static void Foo(int x,int y)
    {
        if(x>y) Console.WriteLine("x>y");
        else if(x==y) Console.WriteLine("x=y");
        else Console.WriteLine("x<y");
    }

    static void Main(string[] args)
    {
        CilGraph graph = typeof(Program).GetMethod("Foo").GetCilGraph();
        Console.WriteLine(graph.ToText());
        Console.ReadKey();
    }
}

/* Output:

.method   public hidebysig static void Foo(
    int32 x,
    int32 y
) cil managed
{
 .maxstack   2
 .locals   init (bool V_0,
    bool V_1)

          nop
          ldarg.0
          ldarg.1
          cgt
          stloc.0
          ldloc.0
          brfalse.s   IL_0001
          ldstr       "x>y"
          call        void [mscorlib]System.Console::WriteLine(string)
          nop
          br.s        IL_0003
 IL_0001: ldarg.0
          ldarg.1
          ceq
          stloc.1
          ldloc.1
          brfalse.s   IL_0002
          ldstr       "x=y"
          call        void [mscorlib]System.Console::WriteLine(string)
          nop
          br.s        IL_0003
 IL_0002: ldstr       "x<y"
          call        void [mscorlib]System.Console::WriteLine(string)
          nop
 IL_0003: ret
}
*/
```
