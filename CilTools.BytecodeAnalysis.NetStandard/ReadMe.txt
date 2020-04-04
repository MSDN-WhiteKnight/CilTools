*** CilTools.BytecodeAnalysis library (.NET Standard version) ***
Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
License: BSD 2.0
Version: 1.3 (23.03.2019)

CilTools.BytecodeAnalysis reads .NET methods' Common Intermediate Language (CIL) bytecode and converts it into high-level objects or textual CIL representation so they can be easily studied and programmatically processed.

Requirements: .NET Standard 2.0+

Usage: Add reference to CilTools.BytecodeAnalysis.dll, import CilTools.BytecodeAnalysis namespace. Use CilReader.GetInstructions to get the collection of instructions from method, CilAnalysis.GetGraph to get a a graph that represents a flow of control between method's instructions, or CilAnalysis.MethodToText when you need to output method's CIL code as text. CilTools.BytecodeAnalysis.Extensions namespace provides an alternative syntax via extenstion methods.

Example:

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
call       void [System.Console]System.Console::WriteLine(string)
nop
ldstr      "{0} + {1} = {2}"
ldloc.0
box        [System.Private.CoreLib]System.Int32
ldloc.1
box        [System.Private.CoreLib]System.Int32
ldloc.0
ldloc.1
add
box        [System.Private.CoreLib]System.Int32
call       void [System.Console]System.Console::WriteLine(string, object, object, object)
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

