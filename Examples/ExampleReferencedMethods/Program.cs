//CIL Tools Example - Find methods referenced by given method 
using System;
using System.Collections.Generic;
using System.Reflection;
using CilTools.BytecodeAnalysis;
using CilTools.BytecodeAnalysis.Extensions;

namespace ExampleReferencedMethods
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
        }
    }
}

/* Example output:

System.Diagnostics.Process.GetCurrentProcess
System.Diagnostics.Process.get_Id
System.Int32.ToString
System.Console.WriteLine
*/
