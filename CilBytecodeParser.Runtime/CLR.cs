using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using Microsoft.Diagnostics.Runtime;
using CilBytecodeParser;

namespace CilBytecodeParser.Runtime
{
    public class CLR
    {        
        static ClrAssemblyInfo GetAssemblyInfo(string modulename, ClrRuntime runtime)
        {
            ClrModule module = null;

            foreach (ClrModule x in runtime.Modules)
            {
                if (x.FileName == null) continue;

                if (Path.GetFileName(x.FileName).Equals(modulename, StringComparison.InvariantCultureIgnoreCase))
                {
                    module = x;
                }
            }

            if (module == null) return null;                        

            return (ClrAssemblyInfo)(new ClrAssemblyReader(runtime).Read(module));
        }

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
            module = Path.GetFileName(process.MainModule.FileName);            

            Console.WriteLine("Process ID: {0}; Process name: {1}", process.Id, module);
            Console.WriteLine();

            //Start ClrMD session
            DataTarget dt = DataTarget.AttachToProcess(process.Id, 5000, AttachFlag.Passive);

            using (dt)
            {
                ClrInfo runtimeInfo = dt.ClrVersions[0];
                ClrRuntime runtime = runtimeInfo.CreateRuntime();
                ClrAssemblyInfo ass = GetAssemblyInfo(module, runtime);

                //dump regular methods

                foreach (var t in runtime.Heap.EnumerateTypes())
                {
                    string name = t.Module.FileName;
                    if (name == null) name = "";                    

                    if (!(Path.GetFileName(name).Equals(module, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }
                    Console.WriteLine(t.Name);

                    foreach (var m in t.Methods)
                    {
                        if (!(m.Type.Name == t.Name)) continue;

                        Console.WriteLine(" Method: " + m.Name);

                        ClrMethodInfo info = new ClrMethodInfo(m, new ClrTypeInfo(t,ass));

                        CilGraph gr = CilAnalysis.GetGraph(info);
                        Console.WriteLine(gr.ToString());

                        Console.WriteLine();
                        Console.ReadKey();
                    }

                    Console.WriteLine();
                }

                //dump dynamic methods

                var en = runtime.Heap.EnumerateObjects();
                ;

                foreach (ClrObject o in en)
                {
                    if (o.Type == null) continue;

                    var bt = o.Type.BaseType;

                    if(o.Type.Name == "System.Reflection.Emit.DynamicMethod" || o.Type.Name == "System.Reflection.Emit.MethodBuilder")
                    {
                        ClrDynamicMethod dm = new ClrDynamicMethod(o);
                        CilGraph gr = CilAnalysis.GetGraph(dm);
                        Console.WriteLine(gr.ToString());
                        Console.WriteLine();
                        Console.ReadKey(); 
                    }
                    
                }
            }//end using
        }
    }
}
