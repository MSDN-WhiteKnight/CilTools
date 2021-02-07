/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using CilView.FileSystem;

namespace CilView.UI.Dialogs
{
    /// <summary>
    /// OpenBclWindow codebehind
    /// </summary>
    public partial class OpenBclWindow : Window
    {
        RuntimeDir[] dirs;
        AssemblyFile selected;

        public OpenBclWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                dirs = RuntimeDir.GetRuntimeDirs();
                cbRuntime.ItemsSource = dirs;

                if (dirs.Length > 0) cbRuntime.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }

        public AssemblyFile SelectedFile { get { return this.selected; } }

        private void bOpen_Click(object sender, RoutedEventArgs e)
        {
            AssemblyFile file = listAssemblies.SelectedItem as AssemblyFile;

            if (file == null)
            {
                MessageBox.Show("Select assembly to open");
                return;
            }

            this.selected = file;
            this.DialogResult = true;
            this.Close();
        }

        private async void cbRuntime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RuntimeDir dir = cbRuntime.SelectedItem as RuntimeDir;
            if (dir == null) return;

            try
            {
                this.Cursor = Cursors.Wait;
                AssemblyFile[] files = await dir.GetAssemblies();
                listAssemblies.ItemsSource = files;
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

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
