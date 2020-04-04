# CIL Tools

![logo](https://msdn-whiteknight.github.io/CilTools/images/IL.png)

CIL tools is a set of software to work with Common Intermediate Language in .NET:

- *CilTools.BytecodeAnalysis* - programmatically inspect bytecode of methods
- *CilTools.Runtime* (work in progress) - load bytecode of methods in another process

## CilTools.BytecodeAnalysis (previously CilBytecodeParser)

**License:** [BSD 2.0](LICENSE)  
**Requirements:** .NET Framework 3.5+  

Download: 

[![Nuget](https://img.shields.io/nuget/v/CilBytecodeParser)](https://www.nuget.org/packages/CilBytecodeParser/) &nbsp; [![GitHub release (latest by date)](https://img.shields.io/github/v/release/MSDN-WhiteKnight/CilTools)](https://github.com/MSDN-WhiteKnight/CilTools/releases)

[View documentation](https://msdn-whiteknight.github.io/CilTools/)

CIL Bytecode Parser reads .NET methods' Common Intermediate Language (CIL) bytecode and converts it into high-level objects or textual CIL representation so they can be easily studied and programmatically processed.

*Usage*

Add reference to CilBytecodeParser.dll, import CilBytecodeParser namespace. Use CilReader.GetInstructions to get the collection of instructions from method, CilAnalysis.GetGraph to get a a graph that represents a flow of control between method's instructions, or CilAnalysis.MethodToText when you need to output method's CIL code as text. CilBytecodeParser.Extensions namespace provides an alternative syntax via extenstion methods.

*Example*

```
using System;
using System.Collections.Generic;
using CilBytecodeParser;
using CilBytecodeParser.Extensions;

namespace CilBytecodeParserTest
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

Copyright (c) 2020,  MSDN.WhiteKnight
