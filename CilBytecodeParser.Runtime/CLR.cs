using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Diagnostics.Runtime;
using CilBytecodeParser;

namespace CilBytecodeParser.Runtime
{
    public class CLR
    {
        public static Dictionary<int, string> GetTokenTable(string module, ClrRuntime runtime)
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

        public static void DumpMethods(int pid)
        {
            //prints bytecode of methods in specified managed process

            string module = "";
            var process = System.Diagnostics.Process.GetProcessById(pid);
            using (process)
            {
                module = Path.GetFileName(process.MainModule.FileName);
            }

            Console.WriteLine("Process ID: {0}; Process name: {1}", pid, module);
            Console.WriteLine();

            //Start ClrMD session
            DataTarget dt = DataTarget.AttachToProcess(pid, 5000, AttachFlag.Passive);

            using (dt)
            {
                ClrInfo runtimeInfo = dt.ClrVersions[0];
                ClrRuntime runtime = runtimeInfo.CreateRuntime();
                Dictionary<int, string> tokens = GetTokenTable(module, runtime);

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

                        ClrMethodInfo info = new ClrMethodInfo(m, dt);                        

                        CilGraph gr = CilAnalysis.GetGraph(info);
                        Console.WriteLine(gr.ToString());

                        Console.WriteLine();
                        Console.ReadKey();
                    }

                    Console.WriteLine();
                }

                //dump dynamic methods

                foreach (ClrObject o in runtime.Heap.EnumerateObjects())
                {
                    if (o.Type == null) continue;
                    if (!o.Type.Name.Contains("ILGenerator")) continue;

                    try
                    {
                        string mname;
                        try
                        {
                            ClrObject builder = o.GetObjectField("m_methodBuilder");
                            mname = builder.GetStringField("m_strName");
                        }
                        catch (ArgumentException)
                        {
                            mname = "???";
                        }

                        int size = o.GetField<int>("m_length");
                        ClrObject stream = o.GetObjectField("m_ILStream");
                        ClrType type = stream.Type;
                        ulong obj = stream.Address;
                        int len = type.GetArrayLength(obj);
                        byte[] il = new byte[size];

                        for (int i = 0; i < size; i++) il[i] = (byte)type.GetArrayElementValue(obj, i);

                        Console.WriteLine("Dynamic method: " + mname);
                        foreach (CilInstruction instr in CilReader.GetInstructions(il))
                        {
                            if (instr.OpCode.FlowControl == FlowControl.Call)
                            {
                                int n = (int)instr.Operand;
                                if (tokens.ContainsKey(n))
                                {
                                    Console.WriteLine(instr.OpCode.ToString().PadRight(10, ' ') + " " + tokens[n]);
                                }
                                else
                                {
                                    Console.WriteLine(instr.OpCode.ToString().PadRight(10, ' ') + " 0x" + n.ToString("X"));
                                }
                            }
                            else Console.WriteLine(instr.ToString());
                        }
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.GetType().ToString() + ": " + ex.Message);
                    }
                }
            }//end using
        }
    }
}
