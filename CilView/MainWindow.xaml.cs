/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using CilTools.Runtime;
using CilTools.Visualization;
using CilView.Build;
using CilView.Common;
using CilView.Core;
using CilView.Core.DocumentModel;
using CilView.Core.Reflection;
using CilView.Core.Syntax;
using CilView.Exceptions;
using CilView.SourceCode;
using CilView.UI.Dialogs;
using CilView.Visualization;

namespace CilView
{
    /// <summary>
    /// CIL View main window codebehind
    /// </summary>
    public partial class MainWindow : Window
    {
        AssemblySource source;
        HistoryContainer<string> recentFiles = new HistoryContainer<string>();
        VisualizationServer srv;

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
            this.srv.Source = newval;
        }

        public MainWindow()
        {
            InitializeComponent();
            this.srv = new VisualizationServer(ServerBase.DefaultUrlHost, ServerBase.DefaultUrlPrefix);
            this.srv.RunInBackground();
            CilVisualization.Server = this.srv;
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

        bool BuildProject(string file, out string assemblyPath) 
        {
            ProgressWindow pwnd;
            assemblyPath = string.Empty;

            //MSBuild project
            BuildProjectOperation opBuild = new BuildProjectOperation(file);
            pwnd = new ProgressWindow(opBuild);
            pwnd.Owner = this;

            //build project in background with progress window
            bool? opres = pwnd.ShowDialog();

            if (opres != true) return false; //cancelled
            if (opBuild.Result == null) return false; //Process.Start error

            bool bres = opBuild.Result.IsSuccessful;

            if (bres)
            {
                //success - open the build output binary
                assemblyPath = opBuild.Result.BinaryPath;
                return true;
            }
            else
            {
                //build error
                wndError wnd = new wndError(
                    "Failed to build code. The build system output is provided below.",
                    opBuild.Result.OutputText
                    );
                wnd.Owner = this;
                wnd.ShowDialog();
                return false;
            }
        }

        void UpdateRecentFilesMenu()
        {
            miRecent.Items.Clear();
            
            foreach (string item in this.recentFiles.Items)
            {
                MenuItem mi = new MenuItem();
                mi.Header = item;
                mi.Click += RecentFilesMenu_Click;
                miRecent.Items.Add(mi);
            }                        
        }

        private void RecentFilesMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;

            if (mi == null) return;

            this.OpenFile(mi.Header.ToString());
        }

        void OpenAssembly(string assemblyPath) 
        {
            ProgressWindow pwnd;
            OpenFileOperation op = new OpenFileOperation(assemblyPath);
            pwnd = new ProgressWindow(op);
            pwnd.Owner = this;
            bool? res = pwnd.ShowDialog();

            if (res != true) return;
            if (op.Result == null) return;

            SetSource(op.Result);

            if (this.source.Assemblies.Count == 1) cbAssembly.SelectedIndex = 0;
        }

        /// <summary>
        /// Loads .il source document in background with a progress indicator
        /// </summary>
        void OpenDocument(string path)
        {
            ProgressWindow pwnd;
            OpenDocumentOperation op = new OpenDocumentOperation(path);
            pwnd = new ProgressWindow(op);
            pwnd.Owner = this;
            bool? res = pwnd.ShowDialog();

            if (res != true) return;
            if (op.Result == null) return;

            this.SetSource(op.Result);

            // IlasmAssemblySource contains a single synthesized assembly
            cbAssembly.SelectedIndex = 0;
        }

        void OpenFile(string file)
        {
            try
            {
                this.recentFiles.Add(file);
                this.UpdateRecentFilesMenu();

                string assemblyPath = String.Empty;

                if (file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
                {
                    //MSBuild project
                    bool bres = BuildProject(file, out assemblyPath);
                    if (bres == false) return;
                }
                else if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".vb", StringComparison.OrdinalIgnoreCase))
                {
                    //Code file
                    string proj = ProjectGenerator.CreateProject(file);

                    bool bres = BuildProject(proj, out assemblyPath);
                    if (bres == false) return;
                }
                else if (FileUtils.HasCilSourceExtension(file))
                {
                    //IL source file
                    this.OpenDocument(file);
                    return; //no need to load assembly
                }
                else
                {
                    //regular assembly
                    assemblyPath = file;
                }

                //open assembly
                this.OpenAssembly(assemblyPath);
            }
            catch (Exception ex) 
            {
                ErrorHandler.Current.Error(ex);
            }
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

                //display assembly manifest
                this.cilbrowser.NavigateToAssembly(ass);

                int c = 0;
                int index = 0;

                for (int i = 0; i < source.Types.Count; i++)
                {
                    if (Utils.StringEquals(source.Types[i].Name, "<Module>")) continue;

                    c++;
                    index = i;

                    if (c >= 2) break;
                }

                //If there's only one non-module type, select it automatically.
                //There's a good chance it's exactly what user needs.

                if (c == 1 && !(ass is IlasmAssembly)) cbType.SelectedIndex = index;
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
            Type t;

            try
            {
                t = (Type)cbType.SelectedItem;

                if (t == null) return;
                if (source == null) return;

                source.Methods.Clear();
                source.Methods = this.cilbrowser.NavigateToType(t, true);
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
                return;
            }

            try
            {
                //count number of methods and other members
                int c_methods = 0;
                int c_others = 0;
                MemberInfo[] members = Utils.GetDeclaredMembers(t);

                for (int i = 0; i < members.Length; i++)
                {
                    if (Utils.IsMethodAndNotConstructor(members[i]))
                    {
                        c_methods++;
                    }
                    else if (!Utils.IsConstructor(members[i]))
                    {
                        c_others++;
                    }

                    if (c_methods >= 2) break;
                    if (c_others >= 1) break;
                }

                //If there's only one non-constructor method, navigate to it.
                //It is what user most likely needs, as there's no other useful info
                //to show about type in this case.

                if (c_methods == 1 && c_others == 0)
                {
                    for (int i = 0; i < this.source.Methods.Count; i++)
                    {
                        if (Utils.IsMethodAndNotConstructor(this.source.Methods[i]))
                        {
                            this.cilbrowser.NavigateToMethod(this.source.Methods[i]);
                            break;
                        }
                    }//end for
                }
            }
            catch (Exception ex)
            {
                //We don't want a visible error here as it only affects the initially 
                //selected method.
                ErrorHandler.Current.Error(ex, "cbType_SelectionChanged", silent:true);
            }
        }

        void OnOpenFileClick()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.RestoreDirectory = true;
            dlg.Filter = "Supported file types|*.exe;*.dll;*.cs;*.vb;*.csproj;*.vbproj;*.il;*.txt|" +
                ".NET Assemblies|*.exe;*.dll|" +
                "Code files|*.cs;*.vb|" +
                "MSBuild projects|*.csproj;*.vbproj|" +
                "IL source files|*.il;*.txt|All files|*";

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

            try
            {
                BuildSystemInvocation.CleanupTempFiles();
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex, "BuildSystemInvocation.CleanupTempFiles", true);
            }
        }

        private void miAbout_Click(object sender, RoutedEventArgs e)
        {
            string text = String.Format(@"CIL View {0}
Windows application to display CIL code of methods in .NET assemblies

Author: MSDN.WhiteKnight
Repository: https://github.com/MSDN-WhiteKnight/CilTools
License: BSD 2.0

This CIL View distribution contains the binary code of the following libraries:

- .NET Libraries (https://github.com/dotnet/runtime/): Copyright (c) .NET Foundation and Contributors, MIT License.
- ClrMD (https://github.com/microsoft/clrmd/): Copyright (c) .NET Foundation and Contributors, MIT License. 
- Newtonsoft.Json (https://github.com/JamesNK/Newtonsoft.Json/): Copyright (c) 2007 James Newton-King, MIT License.

See Help - Credits for more information.
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
            if (!this.cilbrowser.HasCurrentObject)
            {
                MessageBox.Show(this, "No content to export. Open type or method first to export its code", "Error");
                return;
            }
            
            string content;
            OutputFormat fmt;

            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.RestoreDirectory = true;
                dlg.DefaultExt = ".il";
                dlg.Filter = "CIL Assembler source (*.il)|*.il|Hypertext document (*.html;*.htm)|*.html;*.htm";
                MethodBase current_method = this.cilbrowser.GetCurrentMethod();

                if (current_method != null) dlg.FileName = current_method.Name;

                if (dlg.ShowDialog(this) != true) return;

                if (dlg.FilterIndex == 2) // HTML
                {
                    content = cilbrowser.GetHtmlContent();
                    fmt = OutputFormat.Html;
                }
                else
                {
                    content = cilbrowser.GetTextContent();
                    fmt = OutputFormat.Plaintext;
                }

                StreamWriter wr = new StreamWriter(dlg.FileName, false, Encoding.UTF8);

                using (wr)
                {
                    await SyntaxWriter.WriteContentAsync(content, wr, fmt);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }

        private async void miExportType_Click(object sender, RoutedEventArgs e)
        {
            Type t = cilbrowser.GetCurrentType();

            if (t == null)
            {
                MessageBox.Show(this, "No content to export. Open type first to export its code", "Error");
                return;
            }

            try
            {
                OutputFormat fmt;
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.RestoreDirectory = true;
                dlg.DefaultExt = ".il";
                dlg.Filter = "CIL Assembler source (*.il)|*.il|Hypertext document (*.html;*.htm)|*.html;*.htm";
                dlg.FileName = t.Name + ".il";

                if (dlg.ShowDialog(this) != true) return;

                if (dlg.FilterIndex == 2) // HTML
                {
                    fmt = OutputFormat.Html;
                }
                else
                {
                    fmt = OutputFormat.Plaintext;
                }

                Mouse.OverrideCursor = Cursors.Wait;
                StreamWriter wr = new StreamWriter(dlg.FileName, false, Encoding.UTF8);

                using (wr)
                {
                    if (t is IlasmType)
                    {
                        IlasmType it = (IlasmType)t;
                        string content;

                        if (fmt == OutputFormat.Html)
                        {
                            HtmlVisualizer vis = new HtmlVisualizer();
                            content = VisualizationServer.VisualizeObject(it, vis, new VisualizationOptions());
                        }
                        else
                        {
                            content = it.GetDocumentText();
                        }
                        
                        await SyntaxWriter.WriteContentAsync(content, wr, fmt);
                    }
                    else
                    {
                        await SyntaxWriter.DisassembleTypeAsync(t, CilVisualization.CurrentDisassemblerParams, 
                            fmt, wr);
                    }
                }

                Mouse.OverrideCursor = null;
                MessageBox.Show(this, "Export is completed successfully", "Information");
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                ErrorHandler.Current.Error(ex);
            }
        }

        private async void miExportAssembly_Click(object sender, RoutedEventArgs e)
        {
            Assembly ass = this.cilbrowser.GetCurrentAssembly();

            if (ass == null)
            {
                MessageBox.Show(this, "No content to export. Open assembly first to export its code", "Error");
                return;
            }

            try
            {
                OutputFormat fmt;
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.RestoreDirectory = true;
                dlg.DefaultExt = ".il";
                dlg.Filter = "CIL Assembler source (*.il)|*.il|Hypertext document (*.html;*.htm)|*.html;*.htm";
                dlg.FileName = ass.GetName().Name + ".il";

                if (dlg.ShowDialog(this) != true) return;

                if (dlg.FilterIndex == 2) // HTML
                {
                    fmt = OutputFormat.Html;
                }
                else
                {
                    fmt = OutputFormat.Plaintext;
                }

                Mouse.OverrideCursor = Cursors.Wait;
                StreamWriter wr = new StreamWriter(dlg.FileName, false, Encoding.UTF8);

                using (wr)
                {
                    if (ass is IlasmAssembly)
                    {
                        IlasmAssembly ia = (IlasmAssembly)ass;
                        string content;

                        if (fmt == OutputFormat.Html)
                        {
                            HtmlVisualizer vis = new HtmlVisualizer();
                            content = VisualizationServer.VisualizeObject(ia, vis, new VisualizationOptions());
                        }
                        else
                        {
                            content = ia.GetDocumentText();
                        }

                        await SyntaxWriter.WriteContentAsync(content, wr, fmt);
                    }
                    else
                    {
                        await SyntaxWriter.DisassembleAsync(ass, CilVisualization.CurrentDisassemblerParams, 
                            fmt, wr);
                    }
                }

                Mouse.OverrideCursor = null;
                MessageBox.Show(this, "Export is completed successfully", "Information");
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
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

        void UpdateDisassembly()
        {
            //recreate disassembly if a method is currently shown
            MethodBase mb = this.cilbrowser.GetCurrentMethod();

            if (mb != null)
            {
                this.cilbrowser.NavigateToMethod(mb);
            }
        }

        private void miIncludeCodeSize_Click(object sender, RoutedEventArgs e)
        {
            //toggle whether to show bytecode size as code comments in disassembly or not
            bool includeCodeSize = miIncludeCodeSize.IsChecked;
            CilVisualization.CurrentDisassemblerParams.IncludeCodeSize = includeCodeSize;

            //recreate disassembly if a method is currently shown
            UpdateDisassembly();
        }

        private void miIncludeSourceCode_Click(object sender, RoutedEventArgs e)
        {
            //toggle whether to show source code as code comments in disassembly or not
            bool includesrc = miIncludeSourceCode.IsChecked;
            CilVisualization.CurrentDisassemblerParams.IncludeSourceCode = includesrc;

            //recreate disassembly if a method is currently shown
            UpdateDisassembly();
        }

        void OnHelpClick()
        {
            try
            {
                Type t = typeof(MainWindow);
                string content = Utils.ReadEmbeddedResource(t.Assembly, t.Namespace, "manual.html");
                HtmlViewWindow wnd = new HtmlViewWindow(content);
                wnd.Owner = this;
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
            const string url = "https://github.com/MSDN-WhiteKnight/CilTools";
            string mes = string.Format("Failed to open URL. Navigate to {0} manually in browser to access source code", url);
            WpfUtils.ShellExecute(url,this,mes);
        }

        private void miFeedback_Click(object sender, RoutedEventArgs e)
        {
            const string url = "https://github.com/MSDN-WhiteKnight/CilTools/issues/new";
            string mes = string.Format("Failed to open URL. Navigate to {0} manually in browser to provide feedback", url);
            WpfUtils.ShellExecute(url,this,mes);
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

        private void miOpenCode_Click(object sender, RoutedEventArgs e)
        {
            OpenCodeWindow wnd = new OpenCodeWindow();
            wnd.Owner = this;

            if (wnd.ShowDialog() != true) return;

            try
            {
                //generate project
                string proj = ProjectGenerator.CreateProjectFromCode(wnd.Code, wnd.SelectedLanguage, "Project");

                //build project
                string assemblyPath = string.Empty;
                bool bres = BuildProject(proj, out assemblyPath);
                if (bres == false) return;

                //open resulting assembly
                this.OpenAssembly(assemblyPath);
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }

        private void miShowSource_Click(object sender, RoutedEventArgs e)
        {
            MethodBase current_method = this.cilbrowser.GetCurrentMethod();

            if (current_method == null)
            {
                MessageBox.Show(this, "No method selected. Select method first to show source code.", "Error");
                return;
            }

            SourceCodeUI.ShowSource(current_method, 0, false);
        }

        private void miShowMethodSource_Click(object sender, RoutedEventArgs e)
        {
            MethodBase current_method = this.cilbrowser.GetCurrentMethod();

            if (current_method == null)
            {
                MessageBox.Show(this, "No method selected. Select method first to show source code.", "Error");
                return;
            }

            SourceCodeUI.ShowSource(current_method, 0, true);
        }

        private void miExecute_Click(object sender, RoutedEventArgs e)
        {
            MethodBase current_method = this.cilbrowser.GetCurrentMethod();

            if (current_method == null)
            {
                MessageBox.Show(this, "No method selected. Select method first to execute it.", "Error");
                return;
            }

            ExecuteWindow.ShowExecuteMethodUI(current_method, this);
        }

        private void miFileProperties_Click(object sender, RoutedEventArgs e)
        {
            Assembly ass = cbAssembly.SelectedItem as Assembly;

            if (ass == null)
            {
                MessageBox.Show(this, "Assembly file is not loaded", "Error");
                return;
            }

            try
            {
                TextViewWindow wnd = new TextViewWindow();
                wnd.Owner = this;
                wnd.Title = "Assembly file properties";
                wnd.Text = AssemblyInfoProvider.GetAssemblyInfo(ass);
                wnd.ShowDialog();
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }

        private void miCredits_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = "";
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string path = Path.Combine(dir, "credits.txt");
                text = File.ReadAllText(path);

                TextViewWindow wnd = new TextViewWindow();
                wnd.Owner = this;
                wnd.Text = text;
                wnd.Title = "Credits";
                wnd.Show();
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }
    }
}
