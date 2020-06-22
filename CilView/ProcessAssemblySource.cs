/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
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
