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
        MethodBase current_method = null;

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
            this.current_method = null;
            
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
            try
            {
                Assembly ass = (Assembly)cbAssembly.SelectedItem;

                if (ass == null) return;
                if (source == null) return;

                source.Types.Clear();
                source.Methods.Clear();
                source.Types = AssemblySource.LoadTypes(ass);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error",MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Type t = (Type)cbType.SelectedItem;

                if (t == null) return;
                if (source == null) return;

                source.Methods.Clear();
                source.Methods = AssemblySource.LoadMethods(t);

                this.tlv = CilVisualization.VisualizeMethodList(source.Methods, Navigated);
                cMethodsList.Child = this.tlv;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                this.current_method = mb;
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
            try
            {
                FrameworkContentElement elem = (FrameworkContentElement)sender;
                MethodBase mb = (MethodBase)(elem.Tag);

                if (mb == null) return;

                this.NavigateToMethod(mb);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void Window_Closed(object sender, EventArgs e)
        {
            SetSource(null);
        }

        private void miAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, 
                "CIL View "+typeof(MainWindow).Assembly.GetName().Version.ToString()+Environment.NewLine+
                "CIL Tools project: https://github.com/MSDN-WhiteKnight/CilTools" + Environment.NewLine +
                "License: BSD 2.0", 
                "About",MessageBoxButton.OK,MessageBoxImage.Information);
        }

        private void miExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void miExportMethod_Click(object sender, RoutedEventArgs e)
        {
            if (tbMainContent.Text == String.Empty)
            {
                MessageBox.Show(this, "No content to export. Open method first to export its code", "Error");
                return;
            }

            try
            {

                SaveFileDialog dlg = new SaveFileDialog();
                dlg.RestoreDirectory = true;
                dlg.DefaultExt = ".il";
                dlg.Filter = "CIL Assembler source (*.il)|*.il|All files|*";

                if (this.current_method != null) dlg.FileName = this.current_method.Name;

                if (dlg.ShowDialog(this) == true)
                {
                    StreamWriter wr = new StreamWriter(dlg.FileName, false, Encoding.UTF8);

                    using (wr)
                    {
                        await wr.WriteAsync(tbMainContent.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void miLicense_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Process.Start(Path.Combine(path, "license.txt"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void miSource_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://github.com/MSDN-WhiteKnight/CilTools");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, 
"Failed to open URL. Navigate to https://github.com/MSDN-WhiteKnight/CilTools manually in browser to access source code"+
                    Environment.NewLine+ Environment.NewLine+ ex.ToString(),
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void miFeedback_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://github.com/MSDN-WhiteKnight/CilTools/issues/new");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
@"Failed to open URL. Navigate to https://github.com/MSDN-WhiteKnight/CilTools/issues/new manually in browser
to provide feedback" +
                    Environment.NewLine + Environment.NewLine + ex.ToString(),
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
