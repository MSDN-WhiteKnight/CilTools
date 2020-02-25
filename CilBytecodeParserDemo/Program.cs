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
    class Program
    {
        public static int f = 2;

        static void Main(string[] args)
        {
            DynamicMethod dm = new DynamicMethod("Method", typeof(void), new Type[] {typeof(string) }, typeof(Program).Module);            
            ILGenerator ilg = dm.GetILGenerator(512);
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldftn,typeof(Console).GetMethod("WriteLine",new Type[]{typeof(string)}));
            ilg.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), new Type[] { typeof(string) }, null);            
            ilg.Emit(OpCodes.Ret);

            var deleg = (Action<string>)dm.CreateDelegate(typeof(Action<string>));                        
            deleg("Hello from System.Reflection.Emit!");

            CilGraph gr = CilAnalysis.GetGraph(dm);
            CilGraphNode[] nodes = gr.GetNodes().ToArray();

            foreach (var node in nodes)
            {
                if (node.Instruction.ReferencedSignature != null)
                {
                    Console.WriteLine(node.Instruction.ReferencedSignature.ReturnType.Type.ToString());
                }
            }
                        
            Console.WriteLine(gr.ToString());

            if (!Console.IsInputRedirected) Console.ReadKey();
            return;
            
            Console.WriteLine("*** CIL Bytecode Parser library demo ***");
            Console.WriteLine("Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) ");
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
                CilGraph graph = CilAnalysis.GetGraph(mi);

                graph.Print(null, true, true, true, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                if (!Console.IsInputRedirected) Console.ReadKey();
                throw;
            }

            if (!Console.IsInputRedirected) Console.ReadKey();
        }

    }
}
