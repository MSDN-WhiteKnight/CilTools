//CIL Tools Example - programmatically parse individual CIL instructions
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CilTools.BytecodeAnalysis;
using CilTools.BytecodeAnalysis.Extensions;

namespace ExampleInstructions
{
    class Program
    {
        static void Main(string[] args)
        {
            MethodBase method = typeof(Path).GetMember("GetExtension")[0] as MethodBase;
            IEnumerable<CilInstruction> instructions = method.GetInstructions();

            foreach (CilInstruction instr in instructions)
            {
                Console.WriteLine("{0} | {1}", instr.ByteOffset.ToString().PadLeft(3), instr.ToString());
            }

            Console.ReadKey();
        }
    }
}

/* Example output:

  0 | ldarg.0
  1 | brtrue.s   2
  3 | ldnull
  4 | ret
  5 | ldarg.0
  6 | call       valuetype [System.Private.CoreLib]System.ReadOnlySpan`1<char> [System.Private.CoreLib]System.MemoryExtensions::AsSpan(string)
 11 | call       valuetype [System.Private.CoreLib]System.ReadOnlySpan`1<char> [System.Private.CoreLib]System.IO.Path::GetExtension(valuetype [System.Private.CoreLib]System.ReadOnlySpan`1<char>)
 16 | newobj     instance void [System.Private.CoreLib]System.String::.ctor(valuetype [System.Private.CoreLib]System.ReadOnlySpan`1<char>)
 21 | ret 
*/
