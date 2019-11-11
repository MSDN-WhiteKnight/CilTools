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
    public class MyClass
    {
        public static string field;

        [MyAttribute()]
        [STAThread]
        public void Foo(List<int> x, int param=0)
        {
            Console.WriteLine(this);

            Program.f = param;
        }
    }

    public class MyAttribute : Attribute
    {
        public MyAttribute()
        {

        }

        public string Name { get; set; }
    }

    class Program
    {
        public static int f;
        static void Test()
        {
            string asspath = "C:\\_\\Projects\\CppCliTest\\Debug\\CppCliTest.exe";
            string type = "C";
            string method = "test_pointer_operations";

            /*Assembly ass = Assembly.LoadFrom(asspath);
            Type t = ass.GetType(type);

            MethodInfo[] methods = t.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
                );*/

            MethodInfo mi = typeof(MyClass).GetMethod("Foo");//methods.Where((x) => { return x.Name == method; }).First();
            
            var graph = mi.GetCilGraph();
            var instr = graph.GetInstructions().ToList();

            /*foreach (var item in instr)
            {
                Console.Write(item.OpCode.ToString()+" ");
                if (item.ReferencedSignature != null)
                {
                    Console.Write(item.ReferencedSignature.ToString());
                }
                if (item.ReferencedLocal != null)
                {
                    Console.Write("{0} {1} {2}", item.ReferencedLocal.LocalIndex, item.ReferencedLocal.LocalType, item.ReferencedLocal.IsPinned);
                }
                Console.WriteLine();
            }*/

            //graph.Print(null,false,false,true,true);
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
