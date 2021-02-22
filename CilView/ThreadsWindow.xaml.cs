/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.IO;
using System.Reflection;
using CilTools.Runtime;
using CilTools.BytecodeAnalysis;

namespace CilView
{
    /// <summary>
    /// ThreadsWindow codebehind
    /// </summary>
    public partial class ThreadsWindow : Window
    {
        ClrThreadInfo[] threads;

        public ThreadsWindow(ClrThreadInfo[] th)
        {
            InitializeComponent();
            this.threads = th;
            cbThread.ItemsSource = threads;

            //left pane takes all space at first
            this.cilbrowser.SetContentWidth(0);
            this.cilbrowser.SetLeftPaneWidth(null);
        }

        private void cbThread_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ClrThreadInfo item = (ClrThreadInfo)cbThread.SelectedItem;

                if (item == null) return;

                this.cilbrowser.NavigateToStackTrace(item);
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }
    }
}
