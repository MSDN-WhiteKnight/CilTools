using System;
using System.Collections.Generic;
using CilBytecodeParser;
using CilBytecodeParser.Extensions;

namespace CilBytecodeParserTest
{    
    class Program
    {
        static void Main(string[] args)
        {            
            IEnumerable<CilInstruction> instructions = typeof(System.IO.Path).GetMethod("GetExtension").GetInstructions();
            
            foreach (CilInstruction instr in instructions)
            {
                Console.WriteLine("{0} | {1}",instr.ByteOffset.ToString().PadLeft(3),instr.ToString());
            }
            Console.ReadKey();
        }

    }
}

/* Output (.NET 3.5)

  0 | ldarg.0
  1 | brtrue.s 2
  3 | ldnull
  4 | ret
  5 | ldarg.0
  6 | call void [mscorlib]System.IO.Path::CheckInvalidPathChars(string)
 11 | ldarg.0
 12 | callvirt instance int32 [mscorlib]System.String::get_Length()
 17 | stloc.0
 18 | ldloc.0
 19 | stloc.1
 20 | br.s 60
 22 | ldarg.0
 23 | ldloc.1
 24 | callvirt instance valuetype System.Char [mscorlib]System.String::get_Chars(int32)
 29 | stloc.2
 30 | ldloc.2
 31 | ldc.i4.s 46
 33 | bne.un.s 23
 35 | ldloc.1
 36 | ldloc.0
 37 | ldc.i4.1
 38 | sub
 39 | beq.s 11
 41 | ldarg.0
 42 | ldloc.1
 43 | ldloc.0
 44 | ldloc.1
 45 | sub
 46 | callvirt instance string [mscorlib]System.String::Substring(int32, int32)
 51 | ret
 52 | ldsfld [mscorlib]System.String::Empty
 57 | ret
 58 | ldloc.2
 59 | ldsfld [mscorlib]System.IO.Path::DirectorySeparatorChar
 64 | beq.s 24
 66 | ldloc.2
 67 | ldsfld [mscorlib]System.IO.Path::AltDirectorySeparatorChar
 72 | beq.s 16
 74 | ldloc.2
 75 | ldsfld [mscorlib]System.IO.Path::VolumeSeparatorChar
 80 | beq.s 8
 82 | ldloc.1
 83 | ldc.i4.1
 84 | sub
 85 | dup
 86 | stloc.1
 87 | ldc.i4.0
 88 | bge.s -68
 90 | ldsfld [mscorlib]System.String::Empty
 95 | ret
*/
