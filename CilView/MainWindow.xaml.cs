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

        private void cb_KeyDown(object sender, KeyEventArgs e)
        {
            //entering text into combo box field opens drop-down box
            if (e.Key != Key.Enter && e.Key != Key.Escape)
            {
                ComboBox cb = (ComboBox)sender;
                if (!cb.IsDropDownOpen) cb.IsDropDownOpen = true;
            }
        }

        private void cb_KeyUp(object sender, KeyEventArgs e)
        {
            //scroll combobox field into left on input
            ComboBox cb = (ComboBox)sender;
            TextBox tb = cb.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            if (tb == null) return;

            tb.ScrollToHome();
        }

        private void cb_LostFocus(object sender, RoutedEventArgs e)
        {
            //scroll combobox field into left on leave
            ComboBox cb = (ComboBox)sender;
            TextBox tb = cb.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            if (tb == null) return;

            tb.ScrollToHome();
        }

        private void cbAssembly_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Assembly ass = (Assembly)cbAssembly.SelectedItem;

                if (ass == null) return;
                if (source == null) return;

                this.Cursor = Cursors.Wait;
                source.Types.Clear();
                source.Methods.Clear();
                source.Types = AssemblySource.LoadTypes(ass);
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
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
                ErrorHandler.Current.Error(ex);
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
                ErrorHandler.Current.Error(ex);
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
                ErrorHandler.Current.Error(ex);
            }
        }

        private void miShowExceptions_Click(object sender, RoutedEventArgs e)
        {
            if (current_method == null)
            {
                MessageBox.Show(this, "No method selected. Select method first to show exceptions.", "Error");
                return;
            }

            StringBuilder sb = new StringBuilder();

            try
            {
                this.Cursor = Cursors.Wait;
                IEnumerable<Type> exceptions = Analysis.GetExceptions(current_method);
                foreach (Type t in exceptions) sb.AppendLine(t.ToString());
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
                return;
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
            }

            TextViewWindow wnd = new TextViewWindow();
            wnd.Owner = this;
            wnd.Title = "Exceptions";
            wnd.Text = sb.ToString();
            wnd.Show();
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
                ErrorHandler.Current.Error(ex);
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
                ErrorHandler.Current.Error(ex);
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
                ErrorHandler.Current.Error(ex);
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
                ErrorHandler.Current.Error(ex);
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
                    Environment.NewLine+ Environment.NewLine+ ex.Message,
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
                    Environment.NewLine + Environment.NewLine + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void OnSearchClick()
        {
            //validation
            if (this.source == null)
            {
                MessageBox.Show(this, "Open file or process first to use search", "Information");
                return;
            }

            string text = tbFind.Text.Trim();

            if (text == String.Empty)
            {
                if (this.source.Methods != null && this.source.Methods.Count > 0)
                {
                    MessageBox.Show(this, "Enter the method, type or assembly name fragment to search",
                        "Information");
                }
                else if (this.source.Types != null && this.source.Types.Count > 0)
                {
                    MessageBox.Show(this, "Enter the type or assembly name fragment to search", 
                        "Information");
                }
                else
                {
                    MessageBox.Show(this, "Enter the assembly name fragment to search",
                        "Information");
                }
                return;
            }

            //actual search
            try
            {
                IEnumerable<SearchResult> searcher = this.source.Search(text);
                System.Windows.Controls.ContextMenu cm = new ContextMenu();

                int i = 0;

                foreach (SearchResult item in searcher)
                {
                    MenuItem mi = new MenuItem();
                    mi.Header = item.Name.Replace("_","__");
                    mi.Tag = item;
                    mi.Click += Mi_Click;
                    cm.Items.Add(mi);
                    i++;

                    if (i >= 20) break; //limit context menu items count
                }

                if (i == 0)
                {
                    MessageBox.Show(this, "No items matching the query \""+text+ "\" were found",
                        "Information");
                    return;
                }

                //show context menu with search results
                cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                cm.PlacementTarget = tbFind;
                cm.IsOpen = true;
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }

        private void Mi_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            SearchResult val = (SearchResult)mi.Tag;

            if (val.Kind == SearchResultKind.Type)
            {
                cbType.SelectedIndex = val.Index;
            }
            else if (val.Kind == SearchResultKind.Assembly)
            {
                cbAssembly.SelectedIndex = val.Index;
            }
            else if (val.Kind == SearchResultKind.Method)
            {
                NavigateToMethod((MethodBase)val.Value);
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
