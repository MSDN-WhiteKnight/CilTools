/* CIL Tools 
 * Copyright (c) 2020, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CilTools.BytecodeAnalysis;
using CilTools.Runtime;
using Microsoft.Diagnostics.Runtime;

namespace CilView
{
    /// <summary>
    /// CIL View main window codebehind
    /// </summary>
    public partial class MainWindow : Window
    {
        DataTarget dt = null;
        ObservableCollection<Assembly> assemblies=new ObservableCollection<Assembly>();
        ObservableCollection<Type> types = new ObservableCollection<Type>();
        ObservableCollection<MethodBase> methods = new ObservableCollection<MethodBase>();

        static void DumpMethods(int pid, TextWriter target)
        {
            Process process = Process.GetProcessById(pid);
            using (process)
            {
                DumpMethods(process, target);
            }
        }

        static void DumpMethods(string processname, TextWriter target)
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
                DumpMethods(process, target);
            }
        }

        static void DumpMethods(Process process, TextWriter target)
        {
            //prints bytecode of methods in specified managed process

            string module = "";
            module = System.IO.Path.GetFileName(process.MainModule.FileName);

            target.WriteLine("Process ID: {0}; Process name: {1}", process.Id, module);
            target.WriteLine();

            foreach (MethodBase m in ClrAssemblyReader.EnumerateModuleMethods(process))
            {
                target.WriteLine(" Method: " + m.DeclaringType.Name + "." + m.Name);

                CilGraph gr = CilAnalysis.GetGraph(m);
                target.WriteLine(gr.ToString());

                target.WriteLine();                
            }

            DynamicMethodsAssembly dynass = ClrAssemblyReader.GetDynamicMethods(process);

            using (dynass)
            {
                foreach (MethodBase m in dynass.EnumerateMethods())
                {
                    target.WriteLine("Method: " + m.DeclaringType.Name + "." + m.Name);

                    CilGraph gr = CilAnalysis.GetGraph(m);
                    target.WriteLine(gr.ToString());

                    target.WriteLine();                    
                }
            }
        }

        ObservableCollection<Assembly> LoadAssemblies(int pid)
        {
            Process process = Process.GetProcessById(pid);
            dt = DataTarget.AttachToProcess(process.Id, 5000, AttachFlag.Passive);

            using (process)
            {
                return LoadAssemblies(dt);
            }
        }

        ObservableCollection<Assembly> LoadAssemblies(string processname)
        {
            Process[] processes = Process.GetProcessesByName(processname);
            if (processes.Length == 0)
            {
                MessageBox.Show("Process not found");
                return new ObservableCollection<Assembly>();
            }

            Process process = processes[0];
            dt = DataTarget.AttachToProcess(process.Id, 5000, AttachFlag.Passive);

            using (process)
            {
                return LoadAssemblies(dt);
            }
        }


        static ObservableCollection<Assembly> LoadAssemblies(DataTarget dt)
        {
            ObservableCollection<Assembly> ret = new ObservableCollection<Assembly>();

            if (dt.ClrVersions.Count == 0)
            {
                MessageBox.Show("Error: unable to find .NET Runtime in target process!");
                return ret;
            }

            var runtimeInfo = dt.ClrVersions[0];
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

        static ObservableCollection<Type> LoadTypes(Assembly ass)
        {
            ObservableCollection<Type> ret = new ObservableCollection<Type>(ass.GetTypes());
            return ret;
        }

        static ObservableCollection<MethodBase> LoadMethods(Type t)
        {
            ObservableCollection<MethodBase> ret = new ObservableCollection<MethodBase>();
            MemberInfo[] members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

            foreach (MemberInfo member in members)
            {
                if (member is MethodBase) ret.Add((MethodBase)member);
            }

            return ret;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void bShow_Click(object sender, RoutedEventArgs e)
        {
            if (this.dt != null)
            {
                this.dt.Dispose();
                this.dt = null;
            }

            this.assemblies.Clear();
            this.types.Clear();
            this.methods.Clear();

            this.assemblies = LoadAssemblies(tbProcessName.Text);
            cbAssembly.ItemsSource = this.assemblies;
        }

        private void cbAssembly_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Assembly ass = (Assembly)cbAssembly.SelectedItem;

            if (ass == null) return;

            this.types.Clear();
            this.methods.Clear();
            this.types = LoadTypes(ass);
            cbType.ItemsSource = this.types;
        }

        private void cbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Type t = (Type)cbType.SelectedItem;

            if (t == null) return;

            this.methods.Clear();
            this.methods = LoadMethods(t);
            lbMethod.ItemsSource = this.methods;
        }

        private void lbMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MethodBase mb = (MethodBase)lbMethod.SelectedItem;

            if (mb == null) return;

            StringBuilder sb = new StringBuilder(1000);
            StringWriter wr = new StringWriter(sb);

            using (wr)
            {
                CilGraph gr = CilAnalysis.GetGraph(mb);
                gr.Print(wr, true, true, true, true);
                wr.Flush();
                tbMainContent.Text = sb.ToString();
            }
        }
    }
}
