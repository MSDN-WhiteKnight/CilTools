# CilTools.BytecodeAnalysis

**Requirements:** .NET Framework 3.5+ or .NET Standard 2.0+

CilTools.BytecodeAnalysis reads .NET methods' Common Intermediate Language (CIL) bytecode and converts it into high-level objects or textual CIL representation so they can be easily studied and programmatically processed. See [Documentation](https://msdn-whiteknight.github.io/CilTools/) and [Examples](https://github.com/MSDN-WhiteKnight/CilTools/blob/master/Examples) for more information.

## Usage

- Import `CilTools.BytecodeAnalysis` namespace
- Use [CilReader.GetInstructions](https://msdn-whiteknight.github.io/CilTools/api/CilTools.BytecodeAnalysis.CilReader.html#CilTools_BytecodeAnalysis_CilReader_GetInstructions_System_Reflection_MethodBase_) to get the collection of instructions from method
- Use [CilGraph.Create](https://msdn-whiteknight.github.io/CilTools/api/CilTools.BytecodeAnalysis.CilGraph.html#CilTools_BytecodeAnalysis_CilGraph_Create_System_Reflection_MethodBase_) to get a graph that represents a flow of control between method's instructions
- Use [CilAnalysis.MethodToText](https://msdn-whiteknight.github.io/CilTools/api/CilTools.BytecodeAnalysis.CilAnalysis.html#CilTools_BytecodeAnalysis_CilAnalysis_MethodToText_System_Reflection_MethodBase_) when you need to output method's CIL code as text
- [CilTools.BytecodeAnalysis.Extensions](https://msdn-whiteknight.github.io/CilTools/api/CilTools.BytecodeAnalysis.Extensions.html) namespace provides an alternative syntax via extension methods

## Example

```
using System;
using System.Collections.Generic;
using CilTools.BytecodeAnalysis;
using CilTools.BytecodeAnalysis.Extensions;

namespace CilToolsExample
{
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

----

Copyright (c) 2022,  MSDN.WhiteKnight
