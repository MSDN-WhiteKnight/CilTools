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
                                /*if (tokens.ContainsKey(n))
                                {
                                    Console.WriteLine(instr.OpCode.ToString().PadRight(10, ' ') + " " + tokens[n]);
                                }
                                else*/
                                {
                                    Console.WriteLine(instr.OpCode.ToString().PadRight(10, ' ') + " 0x" + n.ToString("X"));
                                }
                            }
                            else Console.WriteLine(instr.ToString());
                        }
                        Console.ReadKey();
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
