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
        public static Dictionary<int, string> GetTokenStringTable(string module, ClrRuntime runtime)
        {
            //get metadata tokens for specified module in ClrMD debugging session

            Dictionary<int, string> table = new Dictionary<int, string>();

            foreach (var t in runtime.Heap.EnumerateTypes())
            {
                string name = t.Module.FileName;
                if (name == null) name = "";
                if (!Path.GetFileName(name).Equals(module, StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                foreach (var m in t.Methods)
                {
                    if (!(m.Type.Name == t.Name)) continue; //skip inherited methods

                    table[(int)m.MetadataToken] = t.Name + "." + m.Name;
                }
            }

            return table;
        }

        static ClrTokenTable GetTokenTable(string module, ClrRuntime runtime)
        {
            //get metadata tokens for specified module in ClrMD debugging session

            ClrTokenTable table = new ClrTokenTable();

            foreach (ClrType t in runtime.Heap.EnumerateTypes())
            {
                string name = t.Module.FileName;
                if (name == null) name = "";
                if (!Path.GetFileName(name).Equals(module, StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                ClrTypeInfo ti = new ClrTypeInfo(t);
                table.SetValue((int)t.MetadataToken, ti);

                foreach (var m in t.Methods)
                {
                    if (!(m.Type.Name == t.Name)) continue; //skip inherited methods

                    table.SetValue((int)m.MetadataToken, new ClrMethodInfo(m, runtime.DataTarget, table));
                }

                foreach (var f in t.Fields)
                {
                    table.SetValue((int)f.Token, new ClrFieldInfo(f,ti));
                }

                foreach (var f in t.StaticFields)
                {
                    table.SetValue((int)f.Token, new ClrFieldInfo(f, ti));
                }

                foreach (var f in t.ThreadStaticFields)
                {
                    table.SetValue((int)f.Token, new ClrFieldInfo(f, ti));
                }
            }

            return table;
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
                ClrTokenTable tokens = GetTokenTable(module, runtime);

                //dump regular methods

                foreach (var t in runtime.Heap.EnumerateTypes())
                {
                    string name = t.Module.FileName;
                    if (name == null) name = "";
                    ;

                    if (!(Path.GetFileName(name).Equals(module, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }
                    Console.WriteLine(t.Name);

                    foreach (var m in t.Methods)
                    {
                        if (!(m.Type.Name == t.Name)) continue;

                        Console.WriteLine(" Method: " + m.Name);

                        ClrMethodInfo info = new ClrMethodInfo(m, dt,tokens);

                        CilGraph gr = CilAnalysis.GetGraph(info);
                        Console.WriteLine(gr.ToString());

                        Console.WriteLine();
                        //Console.ReadKey();
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
                    
                    //if (!o.Type.Name.Contains("ILGenerator")) continue;

                    if(o.Type.Name == "System.Reflection.Emit.DynamicMethod" || o.Type.Name == "System.Reflection.Emit.MethodBuilder")
                    {
                        ClrDynamicMethod dm = new ClrDynamicMethod(o, tokens);
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
