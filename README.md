# CIL Tools

![logo](https://msdn-whiteknight.github.io/CilTools/images/IL.png)

**License:** [BSD 2.0](https://gitflic.ru/project/smallsoft/ciltools/blob?file=LICENSE&branch=master)

[Documentation](https://msdn-whiteknight.github.io/CilTools/) | [Examples](https://gitflic.ru/project/smallsoft/ciltools/file?file=Examples&branch=master)

CIL tools is a set of software to work with Common Intermediate Language in .NET:

- *CilTools.BytecodeAnalysis* - programmatically inspect bytecode of methods
- *CilTools.Runtime* - load bytecode of methods in another process
- *CilTools.Metadata* - inspect assembly via reflection without loading it into the current process
- *CilTools.CommandLine* - cross-platform command line tool to display CIL code of methods
- *CilView* - windows application to display CIL code of methods in the given assembly file or process

## CilTools.BytecodeAnalysis (previously CilBytecodeParser)

**Requirements:** .NET Framework 3.5+ or .NET Standard 2.0+ 

[![Nuget](https://img.shields.io/nuget/v/CilTools.BytecodeAnalysis)](https://www.nuget.org/packages/CilTools.BytecodeAnalysis/) &nbsp; [![GitHub release (latest by date)](https://img.shields.io/github/v/release/MSDN-WhiteKnight/CilTools)](https://github.com/MSDN-WhiteKnight/CilTools/releases)

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

## CilTools.Runtime

**Requirements:** .NET Framework 4.5+

[![Nuget](https://img.shields.io/nuget/v/CilTools.Runtime)](https://www.nuget.org/packages/CilTools.Runtime/)

CilTools.Runtime loads CIL bytecode of methods in external process's CLR instance using ClrMD. This enables processing bytecode from external process with CilTools.BytecodeAnalysis library.

## CilTools.Metadata

**Requirements:** .NET Framework 4.5+ or .NET Standard 2.0+

[![Nuget](https://img.shields.io/nuget/v/CilTools.Metadata)](https://www.nuget.org/packages/CilTools.Metadata/)

The library that supports inspecting the contents of .NET assembly via reflection without loading it into the current process. This enables inspecting assemblies for another target framework (such as .NET Standard assemblies when your application is on .NET Framework) or when some dependencies could not be resolved. This also means assemblies can be unloaded from memory when they are no longer needed.

## CilTools.CommandLine

**Requirements:** .NET Core 3.1+

[![Nuget](https://img.shields.io/nuget/v/CilTools.CommandLine)](https://www.nuget.org/packages/CilTools.CommandLine/)

Cross-platform command line tool to view disassembled CIL code of methods in .NET assemblies. The application runs on any operating system supported by .NET Core. CilTools.CommandLine could print disassembled CIL into the console (standard output) or .il file. Syntax highlighting is supported for console output as long as the target console implementation supports setting console colors. 

Install as global .NET tool:

    dotnet tool install --global CilTools.CommandLine
    
For more information see [readme file](https://github.com/MSDN-WhiteKnight/CilTools/blob/master/CilTools.CommandLine/readme.md).

## CilView

**Requirements:** .NET Framework 4.5+

**Download:** [ClickOnce installer](https://msdn-whiteknight.github.io/CilTools/update/), [Releases](https://github.com/MSDN-WhiteKnight/CilTools/releases)

A windows application to display CIL code of methods in the given assembly file or process. Supports syntax highlighting and navigating to method code by clicking on its reference.

![cilview](https://raw.githubusercontent.com/MSDN-WhiteKnight/CilTools/master/docfx_project/images/cilview.png)

---

Copyright (c) 2023,  MSDN.WhiteKnight
