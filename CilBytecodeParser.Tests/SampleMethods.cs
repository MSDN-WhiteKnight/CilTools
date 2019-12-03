/* CilBytecodeParser library unit tests
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace CilBytecodeParser.Tests
{
    public static class SampleMethods
    {
        public static void PrintHelloWorld()
        {
            Console.WriteLine("Hello, World");
        }

        public static double CalcSum(double x, double y)
        {
            return x + y;
        }

        public static int Foo = 2;

        public static void SquareFoo()
        {
            Foo = Foo * Foo;
        }

        public static int GetInterfaceCount(Type t)
        {
            Type[] array = t.GetInterfaces();
            return array.Length;
        }

        public static void PrintList()
        {
            List<string> lst = new List<string>();
            lst.Add("Bob");
            lst.Add("Alice");
            Console.WriteLine(String.Join(";", lst));
        }

        public static T[] GenerateArray<T>(int len)
        {
            return new T[len];
        }

        public static void PrintTenNumbers()
        {
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(i);
            }
        }

        public static void PrintProcessId()
        {
            int x = System.Diagnostics.Process.GetCurrentProcess().Id;
            Console.WriteLine(x.ToString());
        }
    }
}
