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
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;

namespace CilView
{
    /// <summary>
    /// CIL View main window codebehind
    /// </summary>
    public partial class MainWindow : Window
    {
        AssemblySource source;
        TextListViewer tlv;

        void SetSource(AssemblySource newval)
        {
            if (this.source != null)
            {
                this.source.Dispose();
                this.source = null;
                this.DataContext = null;
            }

            if(tlv!=null)tlv.Clear();

            tbCurrLocation.Text = String.Empty;
            tbMainContent.Text = String.Empty;
            gridStructure.Children.Clear();
            
            this.source = newval;
            this.DataContext = newval;
        }

        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void bOpenProcess_Click(object sender, RoutedEventArgs e)
        {
            SelectProcessWindow wnd = new SelectProcessWindow();
            wnd.Owner = this;
            bool? res = wnd.ShowDialog();

            if (res == true)
            {
                OpenProcessOperation op = new OpenProcessOperation(wnd.SelectedProcess, wnd.ActiveMode);
                ProgressWindow pwnd = new ProgressWindow(op);
                pwnd.Owner = this;
                res = pwnd.ShowDialog();

                if (res != true) return;

                SetSource(op.Result);
             }
        }

        private void cbAssembly_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Assembly ass = (Assembly)cbAssembly.SelectedItem;

            if (ass == null) return;
            if (source == null) return;

            source.Types.Clear();
            source.Methods.Clear();
            source.Types = AssemblySource.LoadTypes(ass);
        }

        private void cbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Type t = (Type)cbType.SelectedItem;

            if (t == null) return;
            if (source == null) return;

            source.Methods.Clear();
            source.Methods = AssemblySource.LoadMethods(t);
            
            this.tlv = CilVisualization.VisualizeMethodList(source.Methods,Navigated);
            ccMethodsList.Child = this.tlv;
        }

        void NavigateToMethod(MethodBase mb)
        {
            StringBuilder sb = new StringBuilder(1000);
            StringWriter wr = new StringWriter(sb);

            using (wr)
            {
                CilGraph gr = CilGraph.Create(mb);
                gr.Print(wr, true, true, true, true);
                wr.Flush();
                tbMainContent.Text = sb.ToString();

                UIElement elem = CilVisualization.VisualizeGraph(gr, Navigated);
                gridStructure.Children.Clear();
                gridStructure.Children.Add(elem);
            }

            sb.Clear();
            
            //select method in method list
            this.tlv.SelectedItem = null;

            for (int i = 0; i < this.tlv.ItemCount; i++)
            {
                Inline item = this.tlv.GetItem(i);
                MethodBase itemmethod = item.Tag as MethodBase;

                if (itemmethod == null) continue;

                if(ReferenceEquals(itemmethod, mb))
                {
                    this.tlv.SelectedIndex = i;
                    break;
                }
            }

            //display method location
            Type t = mb.DeclaringType;
            Assembly ass=null;
            if(t!=null) ass = t.Assembly;

            if (ass != null) sb.Append(ass.GetName().Name);
            else sb.Append("???");

            sb.Append(" / ");

            if (t != null) sb.Append(t.FullName);
            else sb.Append("???");

            sb.Append(" / ");
            sb.Append(mb.Name);

            tbCurrLocation.Text = sb.ToString();
        }

        void Navigated(object sender, RoutedEventArgs e)
        {
            FrameworkContentElement elem = (FrameworkContentElement)sender;
            MethodBase mb = (MethodBase)(elem.Tag);

            if (mb == null) return;

            this.NavigateToMethod(mb);
        }

        private void bOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.RestoreDirectory = true;
            dlg.Filter = ".NET Assemblies (*.exe,*.dll)|*.exe;*.dll|All files|*";

            if (dlg.ShowDialog(this) == true)
            {
                OpenFileOperation op = new OpenFileOperation(dlg.FileName);
                ProgressWindow pwnd = new ProgressWindow(op);
                pwnd.Owner = this;
                bool? res = pwnd.ShowDialog();

                if (res != true) return;

                SetSource(op.Result);

                if (this.source.Assemblies.Count == 1) cbAssembly.SelectedIndex = 0;
            }
        }

        private void Window_Closed_1(object sender, EventArgs e)
        {
            SetSource(null);
        }

        private void miAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, 
                "CIL View "+typeof(MainWindow).Assembly.GetName().Version.ToString()+Environment.NewLine+
                "CIL Tools project"+Environment.NewLine+
                "License: BSD 2.0", 
                "About");
        }

        private void miExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
