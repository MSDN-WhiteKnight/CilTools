/* CilBytecodeParser library demo applciation
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using CilBytecodeParser;
using CilBytecodeParser.Extensions;

namespace CilBytecodeParserDemo
{
    class MyClass<T> where T:new()
    {
        public static void Foo<U>(List<U> x, T y)
        {
            List<U> list = new List<U>(x);
            T obj = new T();
        }
    }

    class Program
    {
        

        static void Test()
        {
            var graph = typeof(MyClass<>).GetMethod("Foo").GetCilGraph();
            var instr = graph.GetInstructions().ToList();
            ;
            Console.WriteLine(graph.ToString());
            Console.ReadKey();
        }
        
        static void Main(string[] args)
        {
            Test();

            Console.WriteLine("*** CIL Bytecode Parser library demo ***");
            Console.WriteLine("Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) ");
            Console.WriteLine();
            string asspath;
            string type;
            string method;
            Assembly ass;

            try
            {

                if (args.Length < 3)
                {
                    Console.WriteLine("Prints CIL code of the specified method");
                    Console.WriteLine("Usage: CilBytecodeParserTest.exe (assembly path) (type full name) (method name)");
                    Console.WriteLine();
                    Console.WriteLine("When called without arguments, prints yourself");
                    Console.WriteLine();

                    asspath = "";
                    type = "CilBytecodeParserDemo.Program";
                    method = "Main";
                    Console.WriteLine(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName);
                    ass = Assembly.GetExecutingAssembly();
                }
                else
                {
                    asspath = args[0];
                    type = args[1];
                    method = args[2];
                    Console.WriteLine(asspath);
                    ass = Assembly.LoadFrom(asspath);
                }
                Console.WriteLine("Method: {0}:{1}", type, method);
                Console.WriteLine();
                Type t = ass.GetType(type);

                MethodInfo[] methods = t.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
                    );

                MethodInfo mi = methods.Where((x) => { return x.Name == method; }).First();
                Console.WriteLine(mi.GetCilText());
                
                
            
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }         

            Console.ReadKey();
        }

    }
}
