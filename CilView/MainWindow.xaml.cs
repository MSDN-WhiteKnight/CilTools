/* CIL Tools 
 * Copyright (c) 2020, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CilTools.BytecodeAnalysis;
using CilTools.Runtime;

namespace CilView
{
    /// <summary>
    /// CIL View main window codebehind
    /// </summary>
    public partial class MainWindow : Window
    {
        public static void DumpMethods(int pid, TextWriter target)
        {
            Process process = Process.GetProcessById(pid);
            using (process)
            {
                DumpMethods(process,target);
            }
        }

        public static void DumpMethods(string processname, TextWriter target)
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

        public static void DumpMethods(Process process, TextWriter target)
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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void bShow_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder(2000);
            StringWriter wr = new StringWriter(sb);

            using (wr)
            {
                DumpMethods(tbProcessName.Text, wr);
            }

            tbMainContent.Text = sb.ToString();
        }
    }
}
