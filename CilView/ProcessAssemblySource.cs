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
using CilTools.Metadata;
using Microsoft.Diagnostics.Runtime;

namespace CilView
{
    sealed class ProcessAssemblySource:AssemblySource
    {
        DataTarget dt;
        Process process;
        OperationBase op;
        HashSet<string> paths=new HashSet<string>();
        ClrAssemblyReader reader;
        AssemblyReader rd;

        public ProcessAssemblySource(Process pr, bool active, OperationBase op = null)
        {
            this.op = op;
            this.Init(pr, active);
        }

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

        static bool IsCoreLib(string name)
        {
            return String.Equals(name, "mscorlib", StringComparison.InvariantCultureIgnoreCase) ||
                String.Equals(name, "System.Private.CoreLib", StringComparison.InvariantCultureIgnoreCase);
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

            reader = new ClrAssemblyReader(runtime);

            double max = runtime.Modules.Count;
            int c = 0;

            foreach (ClrModule x in runtime.Modules)
            {
                string path = x.Name;
                if (path == null) path = "";

                if (path != "")
                {
                    string dir = Path.GetDirectoryName(path).ToLower();

                    if(!dir.Contains("assembly\\gac"))this.paths.Add(dir);
                }

                if (op != null)
                {
                    op.Window.ReportProgress("Loading " + Path.GetFileName(path) + "...", c, max);
                    op.DoEvents();
                    if (op.Stopped) return new ObservableCollection<Assembly>(ret);
                }

                Assembly ass=null;
                string name = Path.GetFileNameWithoutExtension(path.Trim());

                //try CilTools.Metadata first
                if (path != "")
                {
                    try
                    {
                        ass = rd.LoadFrom(path);
                    }
                    catch (FileNotFoundException) { }
                    catch (InvalidOperationException) { }
                }

                //if failed, try ClrAssemblyReader

                if (ass == null)
                {
                    ass = reader.Read(x);
                }
                else
                {
                    //add assembly to preloaded, so some CilTools.Runtime parts will avoid the 
                    //expensive AssemblyReader.Read calls
                    reader.AddPreloadedAssembly(ass);
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

        void Init(DataTarget dtSource)
        {
            this.dt = dtSource;
            this.Assemblies = LoadAssemblies(dtSource,op);
        }

        void Init(Process pr, bool active)
        {
            this.Types = new ObservableCollection<Type>();
            this.Methods = new ObservableCollection<MethodBase>();
            this.process = pr;
            this.rd = new AssemblyReader();
            this.rd.AssemblyResolve += Rd_AssemblyResolve;

            try
            {
                string mainmodule = pr.MainModule.FileName;
                this.paths.Add(Path.GetDirectoryName(mainmodule).ToLower());
            }
            catch (NotSupportedException) { }
            catch (Win32Exception) { }
            catch (InvalidOperationException) { }

            AttachFlag at;

            if (active) at = AttachFlag.NonInvasive;
            else at = AttachFlag.Passive;
            
            DataTarget dt = DataTarget.AttachToProcess(pr.Id, 5000, at);
            this.Init(dt);
        }

        private Assembly Rd_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName an = new AssemblyName(args.Name);
            Assembly ret = null;

            foreach (string dir in this.paths)
            {
                string path = Path.Combine(dir, an.Name + ".dll");

                try
                {
                    if (File.Exists(path)) ret = this.rd.LoadFrom(path);
                }
                catch (FileNotFoundException) { }                
                catch (BadImageFormatException) { }
                catch (InvalidOperationException) { }

                if (ret != null) return ret;
            }

            return null;
        }

        public override bool HasProcessInfo
        {
            get { return true; }
        }

        public override string GetProcessInfoString()
        {
            if (this.dt == null) throw new ObjectDisposedException("Data target");

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Process ID: " + dt.ProcessId.ToString());
            sb.AppendLine("Process name: " + process.ProcessName);

            string mainmodule="";
            string descr = "";
            string fver = "";
            string pver = "";
            string vendor = "";

            try
            {
                mainmodule = process.MainModule.FileName;
                
                FileVersionInfo vinfo = process.MainModule.FileVersionInfo;
                descr = vinfo.FileDescription;
                vendor = vinfo.CompanyName;
                fver = vinfo.FileVersion;
                pver = vinfo.ProductVersion;
            }
            catch (NotSupportedException) { }
            catch (Win32Exception) { }
            catch (InvalidOperationException) { }

            sb.AppendLine("Program location: " + mainmodule);
            sb.AppendLine("Description: " + descr);
            sb.AppendLine("Vendor: " + vendor);
            sb.AppendLine("File version: " + fver);
            sb.AppendLine("Product version: " + pver);

            ClrInfo runtimeInfo = dt.ClrVersions[0];
            ClrRuntime runtime = runtimeInfo.CreateRuntime();
            sb.AppendLine("CLR type: " + runtimeInfo.Flavor.ToString());
            sb.AppendLine("CLR location: " + runtimeInfo.ModuleInfo.FileName);
            sb.AppendLine("CLR version: " + runtimeInfo.Version.ToString());

            if (runtime.ServerGC) sb.AppendLine("GC type: Server");
            else sb.AppendLine("GC type: Workstation");

            sb.AppendLine("AppDomain count: " + runtime.AppDomains.Count.ToString());

            sb.AppendLine();

            sb.AppendLine("Managed threads:");
            sb.AppendLine();

            if (this.reader == null) this.reader = new ClrAssemblyReader(runtime);

            ClrThreadInfo[] threads = ClrThreadInfo.Get(runtime, this.Assemblies, this.reader);
            StringWriter wr = new StringWriter(sb);

            for (int i = 0; i < threads.Length; i++)
            {
                wr.Write('-');
                threads[i].Print(wr);
                wr.WriteLine();
            }

            return sb.ToString();
        }

        public override ClrThreadInfo[] GetProcessThreads()
        {
            if (this.dt == null) throw new ObjectDisposedException("Data target");
            
            ClrInfo runtimeInfo = dt.ClrVersions[0];
            ClrRuntime runtime = runtimeInfo.CreateRuntime();
            
            if (this.reader == null) this.reader = new ClrAssemblyReader(runtime);

            ClrThreadInfo[] threads = ClrThreadInfo.Get(runtime, this.Assemblies, this.reader);
            return threads;
        }

        public override void Dispose()
        {
            if (this.dt != null)
            {
                reader.ClearPreloadedAssemblies();
                this.dt.Dispose();
                this.process.Dispose();
                this.Assemblies.Clear();
                this.Types.Clear();
                this.Methods.Clear();
                this.rd.Dispose();
                this.dt = null;
            }
        }
    }
}
