using System;
using CilBytecodeParser.Extensions;

namespace CilBytecodeParserTest
{    
    class Program    
    {
        public static void Foo()
        {
            for (int i = 0; i < 10; i++)
            {                
                Console.WriteLine(i);            
            }            
        }       

        static void Main(string[] args)
        {            
            Console.WriteLine(typeof(Program).GetMethod("Foo").GetCilText());  
            Console.ReadKey();
        }

    }
}

/* Output:

.method public hidebysig static void Foo() cil managed {
 .maxstack 2
 .locals init (int32 V_0, bool V_1)
          nop
          ldc.i4.0
          stloc.0
          br.s  IL_0002
 IL_0001: nop
          ldloc.0
          call void [mscorlib]System.Console::WriteLine(int32)
          nop
          nop
          ldloc.0
          ldc.i4.1
          add
          stloc.0
 IL_0002: ldloc.0
          ldc.i4.s 10
          clt
          stloc.1
          ldloc.1
          brtrue.s  IL_0001
          ret
}
*/