// CilBytecodeParser example: Reading methods' bytecode from managed process using ClrMD

using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Diagnostics.Runtime;
using CilBytecodeParser;

// Required NuGet package: Microsoft.Diagnostics.Runtime

namespace Example_ClrMD
{
    class Program
    {
        public static Dictionary<int, string> GetTokenTable(string module,ClrRuntime runtime)
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

                    if (!(Path.GetFileName(name).Equals(module, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }
                    Console.WriteLine(t.Name);

                    foreach (var m in t.Methods)
                    {
                        if (!(m.Type.Name == t.Name)) continue;

                        Console.WriteLine(" Method: " + m.Name);
                        byte[] il;
                        int bytesread;
                        var ildata = m.IL;

                        if (ildata == null)
                        {
                            Console.WriteLine("Cannot load IL!");
                            Console.WriteLine();
                            continue;
                        }
                        else
                        {
                            il = new byte[ildata.Length];
                            dt.ReadProcessMemory(ildata.Address, il, ildata.Length, out bytesread);
                        }

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
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Enter process ID>");
            int id; 
            if (!Int32.TryParse(Console.ReadLine(), out id))
            {
                Console.WriteLine("Incorrect process ID entered");
            }
            else
            {
                DumpMethods(id);
            }
                        
            Console.ReadKey();
        }
    }
}

/* Example output:

Enter process ID>
9092
Process ID: 9092; Process name: ConsoleApp1.exe

ConsoleApp1.Program
 Method: .ctor
ldarg.0
call       0xA00001D
nop
ret

 Method: GenerateCalcSum
[...]

 Method: Main
nop
call       ConsoleApp1.Program.GenerateCalcSum
stloc.0
ldloc.0
ldc.i4.1
ldc.i4.2
callvirt   0xA000019
stloc.1
ldloca.s   V_1
call       0xA00001A
call       0xA00001B
nop
call       0xA00001C
pop
ret


Dynamic method: ???
ldarg.0
ldarg.1
add
ret

*/
