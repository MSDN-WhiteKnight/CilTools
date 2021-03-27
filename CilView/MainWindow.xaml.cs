/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
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
using System.Windows.Input;
using System.Windows.Documents;
using CilTools.BytecodeAnalysis;
using CilTools.Runtime;
using CilView.Exceptions;
using CilView.UI.Dialogs;
using CilView.UI.Controls;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Win32;

namespace CilView
{
    /// <summary>
    /// CIL View main window codebehind
    /// </summary>
    public partial class MainWindow : Window
    {
        AssemblySource source;

        void SetSource(AssemblySource newval)
        {
            AssemblySource.TypeCacheClear();
            this.cbType.SelectedItem = null;
            this.cbAssembly.SelectedItem = null;
            this.cilbrowser.Clear();
            this.DataContext = null;

            if (this.source != null)
            { 
                this.source.Dispose();
                this.source = null;
            }
            
            this.source = newval;
            this.DataContext = newval;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
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

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                Exception ex = e.ExceptionObject as Exception;
                if (ex != null)
                {
                    ErrorHandler.Current.Error(ex);
                }
                else
                {
                    ErrorHandler.Current.Error(new ApplicationException(e.ExceptionObject.ToString()));
                }
            }));
        }

        void OpenFile(string file)
        {
            OpenFileOperation op = new OpenFileOperation(file);
            ProgressWindow pwnd = new ProgressWindow(op);
            pwnd.Owner = this;
            bool? res = pwnd.ShowDialog();

            if (res != true) return;
            if (op.Result == null) return;

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

        private void miOpenBCL_Click(object sender, RoutedEventArgs e)
        {
            OpenBclWindow wnd = new OpenBclWindow();
            wnd.Owner = this;
            if (wnd.ShowDialog() != true) return;

            string path = wnd.SelectedFile.Path;
            this.OpenFile(path);
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
                source.Methods = this.cilbrowser.NavigateToType(t);
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }

        void OnOpenFileClick()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.RestoreDirectory = true;
            dlg.Filter = ".NET Assemblies (*.exe,*.dll)|*.exe;*.dll|All files|*";

            if (dlg.ShowDialog(this) == true)
            {
                this.OpenFile(dlg.FileName);
            }
        }

        private void bOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OnOpenFileClick();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SetSource(null);
        }

        private void miAbout_Click(object sender, RoutedEventArgs e)
        {
            string text = String.Format(@"CIL View {0}
Windows application to display CIL code of methods in .NET assemblies

Author: MSDN.WhiteKnight
Repository: https://github.com/MSDN-WhiteKnight/CilTools
License: BSD 2.0

This CIL View distribution contains binary code of the ClrMD library (https://github.com/microsoft/clrmd); Copyright (c) .NET Foundation and Contributors, MIT License.
", 
typeof(MainWindow).Assembly.GetName().Version.ToString());

            TextViewWindow wnd = new TextViewWindow();
            wnd.Owner = this;
            wnd.Text = text;
            wnd.Title = "About";
            wnd.Height = 270;
            wnd.Show();
        }

        private void miExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void miExportMethod_Click(object sender, RoutedEventArgs e)
        {
            string txt = cilbrowser.GetTextContent();

            if (String.IsNullOrEmpty(txt))
            {
                MessageBox.Show(this, "No content to export. Open type or method first to export its code", "Error");
                return;
            }

            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.RestoreDirectory = true;
                dlg.DefaultExt = ".il";
                dlg.Filter = "CIL Assembler source (*.il)|*.il|All files|*";
                MethodBase current_method= this.cilbrowser.GetCurrentMethod();

                if (current_method != null) dlg.FileName = current_method.Name;

                if (dlg.ShowDialog(this) == true)
                {
                    StreamWriter wr = new StreamWriter(dlg.FileName, false, Encoding.UTF8);

                    using (wr)
                    {
                        await wr.WriteAsync(txt);
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
            MethodBase current_method = this.cilbrowser.GetCurrentMethod();

            if (current_method == null)
            {
                MessageBox.Show(this, "No method selected. Select method first to show exceptions.", "Error");
                return;
            }

            StringBuilder sb = new StringBuilder();

            try
            {
                this.Cursor = Cursors.Wait;
                IEnumerable<ExceptionInfo> exceptions = ExceptionInfo.GetExceptions(current_method);
                foreach (ExceptionInfo ex in exceptions) sb.AppendLine(ex.ToString());
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

        private async void miCompareExceptions_Click(object sender, RoutedEventArgs e)
        {
            await CompareExceptions();
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

        void OnHelpClick()
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

        private void miHelp_Click(object sender, RoutedEventArgs e)
        {
            OnHelpClick();
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
                this.cilbrowser.NavigateToMethod((MethodBase)val.Value);
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

        void ShowExceptionsType()
        {
            Type current_type = this.cilbrowser.GetCurrentType();

            if (current_type == null)
            {
                MessageBox.Show("Select type to show exceptions", "Error");
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                TypeExceptionInfo tei;
                tei = TypeExceptionInfo.GetFromType(current_type);

                StringBuilder sb = new StringBuilder(5000);
                sb.AppendLine(tei.TypeName);
                sb.AppendLine();

                foreach (string key in tei.Methods)
                {
                    string[] arr = tei.GetExceptions(key);
                    sb.AppendLine(key);
                    sb.AppendLine(arr.Length.ToString() + " exceptions");
                    sb.AppendLine(String.Join(";", arr));
                    sb.AppendLine();
                }

                TextViewWindow wnd = new TextViewWindow();
                wnd.Owner = this;
                wnd.Text = sb.ToString();
                wnd.Title = "Exceptions from current type";
                wnd.Show();
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        async Task CompareExceptions()
        {
            Type current_type = this.cilbrowser.GetCurrentType();

            if (current_type == null)
            {
                MessageBox.Show("Select type to compare exceptions", "Error");
                return;
            }

            string path = "";

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.RestoreDirectory = true;
            dlg.Filter = "XML document (*.xml)|*.xml|All files|*";
            dlg.Title = "Select XML type documentation file";

            if (dlg.ShowDialog(this) != true) return;

            path = dlg.FileName;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                TypeExceptionInfo tei=null;

                await Common.Utils.RunInBackground(() => {
                    tei = TypeExceptionInfo.GetFromXML(path, current_type);
                });
                
                StringBuilder sb = new StringBuilder(5000);
                sb.AppendLine(tei.TypeName);
                sb.AppendLine();

                StringWriter wr = new StringWriter(sb);
                TypeExceptionInfo teiFromCode;
                teiFromCode = TypeExceptionInfo.GetFromType(current_type);

                await Common.Utils.RunInBackground(() =>
                {
                    TypeExceptionInfo.Compare(tei, teiFromCode, wr);
                });

                TextViewWindow wnd = new TextViewWindow();
                wnd.Owner = this;
                wnd.Text = sb.ToString();
                wnd.Title = "Compare exceptions";
                wnd.Show();
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void miShowExceptionsType_Click(object sender, RoutedEventArgs e)
        {
            this.ShowExceptionsType();
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.O &&
                (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                )
            {
                this.OnOpenFileClick();
            }
            else if (e.Key == Key.F1)
            {
                this.OnHelpClick();
            }
            else if (e.Key == Key.F2)
            {
                //show exceptions (type)
                this.ShowExceptionsType();
            }
            else if (e.Key == Key.F4)
            {
                //compare exceptions
                await this.CompareExceptions();
            }
        }
    }
}
