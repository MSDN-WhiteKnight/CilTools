/* CilBytecodeParser library demo application
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
    class MyClass
    {
        public static string field;
        
        public static void Foo(List<int> x, int param)
        {
            //Console.WriteLine(this);
            
            Program.f = 1;
        }
    }

    class Program
    {
        public static int f;
        static void Test()
        {
            //System.Runtime.CompilerServices.CallConvStdcall

            var graph = typeof(MyClass).GetMethod("Foo",BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).GetCilGraph();
            var instr = graph.GetInstructions().ToList();
            
            Console.WriteLine(graph.ToString());
            Console.ReadKey();
        }
        
        static void Main(string[] args)
        {
            //Test();

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
                    Console.WriteLine("Usage: CilBytecodeParserDemo.exe (assembly path) (type full name) (method name)");
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
