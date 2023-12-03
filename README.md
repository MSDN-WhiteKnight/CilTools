# CIL Tools

![logo](https://msdn-whiteknight.github.io/CilTools/images/IL.png)

**License:** [BSD 2.0](https://gitflic.ru/project/smallsoft/ciltools/blob?file=LICENSE&branch=master)

[![gitflic-catalog](https://img.shields.io/badge/gitflic--catalog-blue)](https://gitflic.ru/project/smallsoft/gitflic-catalog) &nbsp; [![GitHub release (latest by date)](https://img.shields.io/github/v/release/MSDN-WhiteKnight/CilTools)](https://github.com/MSDN-WhiteKnight/CilTools/releases)

[Documentation](https://msdn-whiteknight.github.io/CilTools/) | [Examples](Examples/)

CIL tools is a set of software to work with Common Intermediate Language in .NET.

Libraries:

- [CilTools.BytecodeAnalysis](https://www.nuget.org/packages/CilTools.BytecodeAnalysis/) - inspect or disassemble bytecode of methods
- [CilTools.Runtime](https://www.nuget.org/packages/CilTools.Runtime/) - inspect assemblies from external .NET process
- [CilTools.Metadata](https://www.nuget.org/packages/CilTools.Metadata/) - inspect assembly via reflection without loading it into the current process
- [CilTools.SourceCode](https://www.nuget.org/packages/CilTools.SourceCode/) - provides APIs that assist in lexical analysis of source code

Applications:

- [CilTools.CommandLine](https://www.nuget.org/packages/CilTools.CommandLine/) - cross-platform command line disassembler tool
- [CIL View](https://gitflic.ru/project/smallsoft/ciltools/file?file=CilView&branch=master) - graphical CIL viewer application for Windows

## Using CilTools.BytecodeAnalysis

**Requirements:** .NET Framework 3.5+ or .NET Standard 2.0+ 

[![Nuget](https://img.shields.io/nuget/v/CilTools.BytecodeAnalysis)](https://www.nuget.org/packages/CilTools.BytecodeAnalysis/)

CilTools.BytecodeAnalysis reads .NET methods' Common Intermediate Language (CIL) bytecode and converts it into high-level objects or textual CIL representation so they can be easily studied and programmatically processed.

*Usage*

Add reference to CilTools.BytecodeAnalysis.dll, import CilTools.BytecodeAnalysis namespace. Use CilReader.GetInstructions to get the collection of instructions from method, CilGraph.Create to get a graph that represents a flow of control between method's instructions, or CilAnalysis.MethodToText when you need to output method's CIL code as text. CilTools.BytecodeAnalysis.Extensions namespace provides an alternative syntax via extension methods.

*Example*

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
ldstr      "Hello, World"
call       void [mscorlib]System.Console::WriteLine(string)
nop
ldstr      "{0} \053 {1} \075 {2}"
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

For more information, see [documentation](https://msdn-whiteknight.github.io/CilTools/articles/using-bytecode-analysis.html).

---

Copyright (c) 2023,  SmallSoft
