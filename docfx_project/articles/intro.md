# CIL Tools

![logo](../images/il.png)

## CilTools.BytecodeAnalysis

**License:** BSD 2.0  
**Requirements:** .NET Framework 3.5+  

CilTools.BytecodeAnalysis library reads .NET methods' Common Intermediate Language (CIL) bytecode and converts it into high-level objects or textual CIL representation so they can be easily studied and programmatically processed.

*Usage*

Add reference to CilTools.BytecodeAnalysis.dll, import CilTools.BytecodeAnalysis namespace. Use <xref:CilTools.BytecodeAnalysis.CilReader.GetInstructions(System.Reflection.MethodBase)> to get the collection of instructions from method, <xref:CilTools.BytecodeAnalysis.CilAnalysis.GetGraph(System.Reflection.MethodBase)> to get a a graph that represents a flow of control between method's instructions, or <xref:CilTools.BytecodeAnalysis.CilAnalysis.MethodToText(System.Reflection.MethodBase)> when you need to output method's CIL code as text. <xref:CilTools.BytecodeAnalysis.Extensions> namespace provides an alternative syntax via extension methods.

*Example*

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
ldstr "Hello, World"
call void [mscorlib]System.Console::WriteLine(string)
nop
ldstr "{0} + {1} = {2}"
ldloc.0
box [mscorlib]System.Int32
ldloc.1
box [mscorlib]System.Int32
ldloc.0
ldloc.1
add
box [mscorlib]System.Int32
call void [mscorlib]System.Console::WriteLine(string, System.Object, System.Object, System.Object)
nop
ret
*/
```

Copyright (c) 2020, MSDN.WhiteKnight
