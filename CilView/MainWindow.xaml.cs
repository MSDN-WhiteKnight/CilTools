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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length >= 2)
            {
                string first = args[1];

                if (first.Length <= 1) return;

                if (first[0] == '-' || first[0] == '/')
                {
                    //option
                    string option = first.Substring(1);
                    string help = "Usage: \n\n\"CilView <filepath>\" - open file";
                    this.Visibility = Visibility.Hidden;

                    if (String.Equals(option, "help", StringComparison.InvariantCulture) ||
                        String.Equals(option, "?", StringComparison.InvariantCulture))
                    {
                        MessageBox.Show(help, "CilView command line");
                    }
                    else
                    {
                        MessageBox.Show("Unrecognized command line\n\n"+help, "CilView command line");
                    }

                    this.Close();
                }
                else
                {
                    //file path
                    this.OpenFile(first);
                }
            }
        }

        void OpenFile(string file)
        {
            OpenFileOperation op = new OpenFileOperation(file);
            ProgressWindow pwnd = new ProgressWindow(op);
            pwnd.Owner = this;
            bool? res = pwnd.ShowDialog();

            if (res != true) return;

            SetSource(op.Result);

            if (this.source.Assemblies.Count == 1) cbAssembly.SelectedIndex = 0;
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
                this.OpenFile(dlg.FileName);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SetSource(null);
        }

        private void miAbout_Click(object sender, RoutedEventArgs e)
        {
            string text = String.Format(@"CIL View {0}
Windows application to display CIL code of methods in the .NET assemblies

Author: MSDN.WhiteKnight
Repository: https://github.com/MSDN-WhiteKnight/CilTools
License: BSD 2.0", typeof(MainWindow).Assembly.GetName().Version.ToString());

            TextViewWindow wnd = new TextViewWindow();
            wnd.Owner = this;
            wnd.Text = text;
            wnd.Title = "About";
            wnd.Height = 250;
            wnd.Show();
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

        private void bProcessInfo_Click(object sender, RoutedEventArgs e)
        {
            string s="";
            this.Cursor = Cursors.Wait;

            try
            {
                s = this.source.GetProcessInfoString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this, ex.GetType().ToString() + ": " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error
                    );
                return;
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
            }

            TextViewWindow wnd = new TextViewWindow();
            wnd.Title = "Process info";
            wnd.Text = s;
            wnd.Owner = this;
            wnd.Show();
        }

        private void miThreads_Click(object sender, RoutedEventArgs e)
        {
            ClrThreadInfo[] threads;
            this.Cursor = Cursors.Wait;

            try
            {
                threads = this.source.GetProcessThreads();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this, ex.GetType().ToString() + ": " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error
                    );
                return;
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
            }

            ThreadsWindow wnd = new ThreadsWindow(threads);
            wnd.Owner = this;
            wnd.Show();
        }

        private void miHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = "";
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string path = Path.Combine(dir, "readme.txt");
                text = File.ReadAllText(path);

                TextViewWindow wnd = new TextViewWindow();
                wnd.Owner = this;
                wnd.Text = text;
                wnd.Title = "CIL View Help";
                wnd.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.GetType().ToString() + ":" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void miLicense_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = "";
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string path = Path.Combine(dir, "license.txt");
                text = File.ReadAllText(path);

                TextViewWindow wnd = new TextViewWindow();
                wnd.Owner = this;
                wnd.Text = text;
                wnd.Title = "License";
                wnd.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.GetType().ToString() + ":" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        void OnSearchClick()
        {
            if (this.source == null)
            {
                MessageBox.Show(this, "Open file or process first to use search", "Information");
                return;
            }

            int start_index;
            string text = tbFind.Text.Trim();

            ObservableCollection<Type> types = this.source.Types;

            try
            {

                if (types != null && types.Count > 0)
                {
                    //if assembly is selected, search for type within that assembly

                    if (text == String.Empty)
                    {
                        MessageBox.Show(this,
                            "Enter the type name fragment into the text field to search within the selected assembly", "Information"
                            );
                        return;
                    }

                    start_index = cbType.SelectedIndex + 1;

                    if (start_index < 0) start_index = 0;
                    if (start_index >= cbType.Items.Count) start_index = 0;

                    for (int i = start_index; i < types.Count; i++)
                    {
                        if (types[i].Name.StartsWith(text))
                        {
                            cbType.SelectedIndex = i;
                            return;
                        }
                    }

                    for (int i = start_index; i < types.Count; i++)
                    {
                        if (types[i].Name.Contains(text))
                        {
                            cbType.SelectedIndex = i;
                            return;
                        }
                    }

                    for (int i = start_index; i < types.Count; i++)
                    {
                        if (types[i].FullName.Contains(text))
                        {
                            cbType.SelectedIndex = i;
                            return;
                        }
                    }

                    if (start_index == 0)
                    {
                        MessageBox.Show(this,
                        "No types matching the query \"" + text + "\" were found in the selected assembly",
                        "Information");
                    }
                    else
                    {
                        MessageBox.Show(this,
                        "The search reached the end of the list when trying to find type \"" + text + "\" in the selected assembly",
                        "Information");
                    }

                    cbType.SelectedItem = null;
                    return;
                }

                //if no assembly selected, search for assembly
                if (this.source.Assemblies == null) return;
                if (this.source.Assemblies.Count <= 1) return;

                if (text == String.Empty)
                {
                    MessageBox.Show(this, "Enter the assembly name fragment into the text field to search for assemblies", 
                        "Information");
                    return;
                }

                ObservableCollection<Assembly> assemblies = this.source.Assemblies;
                start_index = cbAssembly.SelectedIndex + 1;

                if (start_index < 0) start_index = 0;
                if (start_index >= cbAssembly.Items.Count) start_index = 0;

                for (int i = start_index; i < assemblies.Count; i++)
                {
                    if (assemblies[i].FullName.Contains(text))
                    {
                        cbAssembly.SelectedIndex = i;
                        return;
                    }
                }
                
                MessageBox.Show(this,
                "No assemblies matching the query \"" + text + "\" were found",
                "Information");
                
                cbAssembly.SelectedItem = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void bFind_Click(object sender, RoutedEventArgs e)
        {
            OnSearchClick();
        }

        private void tbFind_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) OnSearchClick();
        }
    }
}
