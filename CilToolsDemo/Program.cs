/* CilTools.BytecodeAnalysis library demo application
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Linq;
using CilTools.BytecodeAnalysis;
using CilTools.BytecodeAnalysis.Extensions;
using CilTools.Runtime;
using CilTools.Metadata;

namespace CilToolsDemo
{
    class Program
    {
        public static void DumpMethods(int pid)
        {
            Process process = Process.GetProcessById(pid);
            using (process)
            {
                DumpMethods(process);
            }
        }

        public static void DumpMethods(string processname)
        {
            Process[] processes = Process.GetProcessesByName(processname);
            if (processes.Length == 0)
            {
                Console.WriteLine("Process not found");
                return;
            }

            Process process = processes[0];

            using (process)
            {
                DumpMethods(process);
            }
        }

        public static void DumpMethods(Process process)
        {
            //prints bytecode of methods in specified managed process

            string module = "";
            module = System.IO.Path.GetFileName(process.MainModule.FileName);
            
            Console.WriteLine("Process ID: {0}; Process name: {1}", process.Id, module);
            Console.WriteLine();

            foreach (MethodBase m in ClrAssemblyReader.EnumerateModuleMethods(process))
            {
                Console.WriteLine(" Method: " + m.DeclaringType.Name + "." + m.Name);

                CilGraph gr = CilGraph.Create(m);
                Console.WriteLine(gr.ToText());

                Console.WriteLine();
                //Console.ReadKey();
            }

            DynamicMethodsAssembly dynass = ClrAssemblyReader.GetDynamicMethods(process);

            using (dynass)
            {
                foreach (MethodBase m in dynass.EnumerateMethods())
                {
                    Console.WriteLine("Method: " + m.DeclaringType.Name + "." + m.Name);

                    CilGraph gr = CilGraph.Create(m);
                    Console.WriteLine(gr.ToText());

                    Console.WriteLine();
                    //Console.ReadKey();
                }
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("*** CIL Tools demo ***");
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
                    Console.WriteLine("Usage: CilToolsDemo.exe (assembly path) (type full name) (method name)");
                    Console.WriteLine();
                    Console.WriteLine("When called without arguments, prints yourself");
                    Console.WriteLine();
                    Console.WriteLine(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName);

                    asspath = "";
                    type = "CilToolsDemo.Program";
                    method = "Main";                    
                    ass = Assembly.GetExecutingAssembly();

                    /*asspath = Assembly.GetExecutingAssembly().Location;
                    type = "CilToolsDemo.Program";
                    method = "Main";
                    ass = new AssemblyReader().LoadFrom(asspath);*/
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

                MemberInfo[] methods = t.GetMembers(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
                    );

                MethodBase mi = methods.OfType<MethodBase>().Where((x) => { return x.Name == method; }).First();
                CilGraph graph = CilGraph.Create(mi);
                
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
