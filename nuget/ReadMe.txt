﻿*** CilBytecodeParser library ***
Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
License: BSD 2.0

CilBytecodeParser reads .NET methods' Common Intermediate Language (CIL) bytecode and converts it into high-level objects or textual CIL representation so they can be easily studied and programmatically processed.

Requirements: .NET Framework 3.5+

Usage: Add reference to CilBytecodeParser.dll, import CilBytecodeParser namespace. Use CilReader.GetInstructions to get the collection of instructions from method, CilAnalysis.GetGraph to get a a graph that represents a flow of control between method's instructions, or CilAnalysis.MethodToText when you need to output method's CIL code as text. CilBytecodeParser.Extensions namespace provides an alternative syntax via extenstion methods.

Example:

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

v1.1
- Fixed types representation when converting instructions to text
- Fixed ldftn instruction representation when converting it to text
- Added CilAnalysis.GetReferencedMembers method

v1.2
- Added proper exception when CilReader encounters unsopprted opcode
- Documented exceptions for CilReader
- Added several new CilAnalysis.GetReferencedMembers overloads
- Added extension methods for GetReferencedMembers

v1.3
- Added support for vararg keyword
- Added support for ldtoken instruction
- Added typedref into short type names
- Fixed local variables and fields representation in generated CIL text
- Fixed types representation in generated CIL text
- Tweaked error handling
- Documented exceptions for most of the API

v1.4
- Added methods for enumerating graph nodes and instructions into CilGraph class
- Added CilInstruction.Parse method
- Added CilInstruction.EmitTo method and corresponding IlGenerator extension method

v1.5
- Moved CilInstruction.opcodes initialization from static constructor to first FindOpCode call, so it will be only loaded when needed.
- Added support for instructions referencing 32-bit target branch operands
- Added special characters escaping when displaying string literals
- Added CilGraph.EmitTo method with jumps support as well as the corresponding IlGenerator extension method

v1.6
- CilGraph.ToString now displays parameter names
- Added support for optional parameters and default values to CilGraph.ToString
- Converting constant values to string now uses InvariantCulture
- Fixed byte, sbyte, float and double type names representation in text output
- Improved representation of array types in text output
