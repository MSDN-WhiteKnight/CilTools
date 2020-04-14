using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using Microsoft.Diagnostics.Runtime;

namespace CilView
{
    /// <summary>
    /// Select Process Window codebehind
    /// </summary>
    public partial class SelectProcessWindow : Window
    {
        Process[] processes;

        public SelectProcessWindow()
        {
            InitializeComponent();
            processes = Process.GetProcesses();
            lbProcesses.ItemsSource = processes;
        }

        public Process SelectedProcess
        {
            get { return lbProcesses.SelectedItem as Process; }
        }

        public bool ActiveMode
        {
            get { return chbActiveMode.IsChecked == true; }
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void bOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
