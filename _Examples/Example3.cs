using System;
using System.Collections.Generic;
using System.Reflection;
using CilBytecodeParser;
using CilBytecodeParser.Extensions;

namespace CilBytecodeParserTest
{    
    class Program
    {
        public static void Test()
        {
            int x = System.Diagnostics.Process.GetCurrentProcess().Id;
            Console.WriteLine(x.ToString());
        }

        static void Main(string[] args)
        {
            IEnumerable<MethodBase> methods = typeof(Program).GetMethod("Test").GetReferencedMethods();

            foreach (MethodBase m in methods)
            {
                Console.WriteLine("{0}.{1}", m.DeclaringType, m.Name);
            }
            Console.ReadKey();
        }

    }
}

/* Output:

System.Diagnostics.Process.GetCurrentProcess
System.Diagnostics.Process.get_Id
System.Int32.ToString
System.Console.WriteLine
*/
