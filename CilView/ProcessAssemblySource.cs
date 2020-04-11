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
            ClrAssemblyReader reader = new ClrAssemblyReader(runtime);

            foreach (ClrModule x in runtime.Modules)
            {
                ret.Add(reader.Read(x));
            }

            DynamicMethodsAssembly dynass = reader.GetDynamicMethods();
            ret.Add(dynass);
            return ret;
        }

        public ProcessAssemblySource(string processname, bool active)
        {
            this.Types = new ObservableCollection<Type>();
            this.Methods = new ObservableCollection<MethodBase>();

            Process[] processes = Process.GetProcessesByName(processname);

            if (processes.Length == 0)
            {
                MessageBox.Show("Process not found");
                this.Assemblies = new ObservableCollection<Assembly>();
                return;
            }

            AttachFlag at;

            if(active) at = AttachFlag.NonInvasive;
            else at = AttachFlag.Passive;

            Process process = processes[0];
            using (process)
            {
                dt = DataTarget.AttachToProcess(process.Id, 5000, at);
                this.Assemblies = LoadAssemblies(dt);
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
