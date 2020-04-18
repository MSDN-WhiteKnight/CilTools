using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CilTools.BytecodeAnalysis;
using CilTools.Runtime;
using Microsoft.Diagnostics.Runtime;
using System.Windows;

namespace CilView
{
    sealed class ProcessAssemblySource:AssemblySource
    {
        DataTarget dt;

        public static ObservableCollection<Assembly> LoadAssemblies(DataTarget dt)
        {
            ObservableCollection<Assembly> ret = new ObservableCollection<Assembly>();

            if (dt.ClrVersions.Count == 0)
            {
                MessageBox.Show("Error: unable to find .NET Runtime in target process!");
                return ret;
            }

            ClrInfo runtimeInfo = dt.ClrVersions[0];
            ClrRuntime runtime = runtimeInfo.CreateRuntime();
            return LoadAssemblies(runtime);
        }

        public static ObservableCollection<Assembly> LoadAssemblies(ClrRuntime runtime)
        {
            ObservableCollection<Assembly> ret = new ObservableCollection<Assembly>();
            ClrAssemblyReader reader = new ClrAssemblyReader(runtime);

            foreach (ClrModule x in runtime.Modules)
            {
                ret.Add(reader.Read(x));
            }

            DynamicMethodsAssembly dynass = reader.GetDynamicMethods();
            ret.Add(dynass);
            return ret;
        }

        void Init(ClrRuntime runtime)
        {
            this.dt = runtime.DataTarget;
            this.Assemblies = LoadAssemblies(runtime);
        }

        public ProcessAssemblySource(ClrRuntime runtime)
        {
            this.Init(runtime);
        }

        void Init(DataTarget dtSource)
        {
            this.dt = dtSource;
            this.Assemblies = LoadAssemblies(dtSource);
        }

        public ProcessAssemblySource(DataTarget dtSource)
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

        public ProcessAssemblySource(Process pr, bool active)
        {
            this.Init(pr, active);
        }
                
        public ProcessAssemblySource(string processname, bool active)
        {
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

        public ProcessAssemblySource(int pid, bool active)
        {
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
