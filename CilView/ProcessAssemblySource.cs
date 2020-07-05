/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using CilTools.BytecodeAnalysis;
using CilTools.Runtime;
using Microsoft.Diagnostics.Runtime;

namespace CilView
{
    sealed class ProcessAssemblySource:AssemblySource
    {
        DataTarget dt;
        OperationBase op;
        HashSet<string> paths=new HashSet<string>();
        HashSet<string> resolved=new HashSet<string>();

        public ObservableCollection<Assembly> LoadAssemblies(DataTarget dt, OperationBase op = null)
        {
            ObservableCollection<Assembly> ret = new ObservableCollection<Assembly>();

            if (dt.ClrVersions.Count == 0)
            {
                MessageBox.Show("Error: unable to find .NET Runtime in target process!");
                return ret;
            }

            ClrInfo runtimeInfo = dt.ClrVersions[0];
            ClrRuntime runtime = runtimeInfo.CreateRuntime();
            return LoadAssemblies(runtime,op);
        }

        Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName an = new AssemblyName(args.Name);
            Assembly ret = null;
            
            foreach (string dir in this.paths)
            {
                string path = Path.Combine(dir, an.Name + ".dll");

                try
                {
                    if (File.Exists(path)) ret = Assembly.ReflectionOnlyLoadFrom(path);
                }
                catch (FileNotFoundException) { }
                catch (FileLoadException) { }
                catch (BadImageFormatException) { }

                if (ret != null) return ret;
            }

            if (resolved.Contains(args.Name)) return null; //prevent stack overflow

            //if failed, resolve by full assembly name
            resolved.Add(args.Name);
            return Assembly.ReflectionOnlyLoad(args.Name);
        }

        public ObservableCollection<Assembly> LoadAssemblies(ClrRuntime runtime, OperationBase op = null)
        {
            List<Assembly> ret = new List<Assembly>();

            if (op != null)
            {
                op.ReportProgress("Loading modules...", 0, 0);
                op.DoEvents();
                if (op.Stopped) return new ObservableCollection<Assembly>(ret);
            }

            ClrAssemblyReader reader = new ClrAssemblyReader(runtime);

            double max = runtime.Modules.Count;
            int c = 0;
            bool added_resolver = false;

            foreach (ClrModule x in runtime.Modules)
            {
                string path = x.Name;
                if (path == null) path = "";

                if (path != "")
                {
                    string dir = Path.GetDirectoryName(path).ToLower();
                    this.paths.Add(dir);
                }

                if (op != null)
                {
                    op.Window.ReportProgress("Loading " + Path.GetFileName(path) + "...", c, max);
                    op.DoEvents();
                    if (op.Stopped) return new ObservableCollection<Assembly>(ret);
                }

                Assembly ass=null;
                string name = Path.GetFileNameWithoutExtension(path.Trim());

                if (path != "" &&  !String.Equals(name, "mscorlib", StringComparison.InvariantCultureIgnoreCase))
                {
                    try
                    {
                        ass = Assembly.ReflectionOnlyLoadFrom(path);

                        if (ass != null && !added_resolver)
                        {
                            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;
                            added_resolver = true;
                        }
                    }
                    catch (FileNotFoundException) { }
                    catch (FileLoadException) { }
                    catch (BadImageFormatException) { }
                    catch (NotSupportedException) { }
                }

                if (ass == null)
                {
                    ass = reader.Read(x);
                }
                ret.Add(ass);
                c++;
            }

            ret.Sort((x, y) => String.Compare(x.FullName, y.FullName, StringComparison.InvariantCulture));

            if (op != null)
            {
                op.Window.ReportProgress("Loading dynamic methods...", c, max);
                op.DoEvents();
                if (op.Stopped) return new ObservableCollection<Assembly>(ret);
            }

            DynamicMethodsAssembly dynass = reader.GetDynamicMethods();
            ret.Add(dynass);
            return new ObservableCollection<Assembly>(ret);
        }

        void Init(ClrRuntime runtime)
        {
            this.dt = runtime.DataTarget;
            this.Assemblies = LoadAssemblies(runtime,op);
        }

        public ProcessAssemblySource(ClrRuntime runtime, OperationBase op = null)
        {
            this.Init(runtime);
        }

        void Init(DataTarget dtSource)
        {
            this.dt = dtSource;
            this.Assemblies = LoadAssemblies(dtSource,op);
        }

        public ProcessAssemblySource(DataTarget dtSource, OperationBase op = null)
        {
            this.Init(dtSource);
        }

        void Init(Process pr, bool active)
        {
            this.Types = new ObservableCollection<Type>();
            this.Methods = new ObservableCollection<MethodBase>();

            AttachFlag at;

            if (active) at = AttachFlag.NonInvasive;
            else at = AttachFlag.Passive;
            
            DataTarget dt = DataTarget.AttachToProcess(pr.Id, 5000, at);
            this.Init(dt);
        }

        public ProcessAssemblySource(Process pr, bool active, OperationBase op = null)
        {
            this.op = op;
            this.Init(pr, active);
        }

        public ProcessAssemblySource(string processname, bool active, OperationBase op = null)
        {
            this.op = op;
            Process[] processes = Process.GetProcessesByName(processname);

            if (processes.Length == 0)
            {
                MessageBox.Show("Process not found");
                this.Assemblies = new ObservableCollection<Assembly>();
                return;
            }

            Process process = processes[0];

            using (process)
            {
                this.Init(process, active);
            }
        }

        public ProcessAssemblySource(int pid, bool active, OperationBase op = null)
        {
            this.op = op;
            Process process = Process.GetProcessById(pid);

            if (process == null)
            {
                MessageBox.Show("Process not found");
                this.Assemblies = new ObservableCollection<Assembly>();
                return;
            }

            using (process)
            {
                this.Init(process, active);
            }
        }

        public override bool HasProcessInfo
        {
            get { return true; }
        }

        public override string GetProcessInfoString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Process ID: " + dt.ProcessId.ToString());
            ClrInfo runtimeInfo = dt.ClrVersions[0];
            ClrRuntime runtime = runtimeInfo.CreateRuntime();
            sb.AppendLine("CLR type: " + runtimeInfo.Flavor.ToString());
            sb.AppendLine("CLR location: " + runtimeInfo.ModuleInfo.FileName);
            sb.AppendLine("CLR version: " + runtimeInfo.Version.ToString());

            if (runtime.ServerGC) sb.AppendLine("GC type: Server");
            else sb.AppendLine("GC type: Workstation");

            sb.AppendLine();

            sb.AppendLine("Managed threads:");
            sb.AppendLine();

            for (int i = 0; i < runtime.Threads.Count; i++)
            {
                if (runtime.Threads[i].IsAlive == false || runtime.Threads[i].IsUnstarted) continue;

                sb.Append("ID: " + runtime.Threads[i].OSThreadId.ToString()+" ");

                if (runtime.Threads[i].IsGC) sb.Append("[GC] ");
                if (runtime.Threads[i].IsFinalizer) sb.Append("[Finalizer] ");
                if (runtime.Threads[i].IsThreadpoolWorker) sb.Append("[Thread pool worker] ");
                if (runtime.Threads[i].IsThreadpoolCompletionPort) sb.Append("[Thread pool completion port] ");
                if (runtime.Threads[i].IsDebuggerHelper) sb.Append("[Debug] ");

                if (runtime.Threads[i].IsMTA) sb.Append("[MTA] ");
                else if (runtime.Threads[i].IsSTA) sb.Append("[STA] ");

                sb.AppendLine();

                IList<ClrStackFrame> stack = runtime.Threads[i].StackTrace;

                for (int j = 0; j < stack.Count; j++)
                {
                    ClrMethod m = stack[j].Method;
                    /*sb.Append(stack[j].DisplayString+" ");
                    if (m != null) sb.Append(m.Name);*/
                    sb.Append(" "+stack[j].ToString());

                    if (m != null)
                    {
                        ulong pos = stack[j].InstructionPointer;

                        ILToNativeMap[] map = m.ILOffsetMap;
                        if (map == null) map = new ILToNativeMap[0];

                        int offset = -1;
                        int offset2 = Int32.MaxValue;
                        bool found = false;

                        for (int k = 0; k < map.Length; k++)
                        {
                            if (pos < map[k].EndAddress && pos >= map[k].StartAddress)
                            {
                                offset = map[k].ILOffset;
                                if(k<map.Length-1) offset2 = map[k + 1].ILOffset;
                                found = true;
                            }
                        }

                        if (found && offset>=0) sb.Append(" +"+offset.ToString());

                        string module = stack[j].ModuleName;
                        Assembly ass = null;

                        for (int k = 0; k < this.assemblies.Count; k++)
                        {
                            string an = this.assemblies[k].GetName().Name;
                            
                            if (String.Equals(an, module, StringComparison.InvariantCultureIgnoreCase))
                            {
                                ass = this.assemblies[k];
                                break;
                            }
                        }

                        Type t=null;
                        MemberInfo mi = null;
                        string tn = m.Type.Name;

                        if (ass != null && ass is ClrAssemblyInfo)
                        {
                            mi = ((ClrAssemblyInfo)ass).ResolveMember((int)m.MetadataToken);
                        }
                        else
                        {
                            if (ass != null && !String.IsNullOrEmpty(tn))
                            {
                                t = ass.GetType(tn);
                            }

                            if (t != null)
                            {
                                MemberInfo[] arr = t.GetMember(m.Name,
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                                    );

                                if (arr.Length == 1)
                                {                                    
                                    mi = arr[0];
                                }
                            }
                        }

                        if (mi != null && mi is MethodBase)
                        {
                            CilGraph gr = CilGraph.Create((MethodBase)mi);
                            CilInstruction[] instructions = gr.GetInstructions().ToArray();

                            /*sb.AppendLine();
                            sb.AppendLine(" IL:");
                            for (int k = 0; k < instructions.Length;k++ )
                            {
                                CilInstruction instr = instructions[k];
                                if (instr.ByteOffset >= offset && instr.ByteOffset<=offset2)
                                {                                    
                                    sb.AppendLine(instr.ToString());
                                } 
                            }
                            sb.AppendLine();*/
                        }
                    }
                    
                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        public override void Dispose()
        {
            if (this.dt != null)
            {
                this.dt.Dispose();
                this.Assemblies.Clear();
                this.Types.Clear();
                this.Methods.Clear();
                this.dt = null;
            }
        }
    }
}
