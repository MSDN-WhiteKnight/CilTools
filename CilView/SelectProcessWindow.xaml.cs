﻿using System;
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
            Array.Sort<Process>(processes, (x, y) => {
                try
                {
                    return x.ProcessName.CompareTo(y.ProcessName);
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            });
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
            if (lbProcesses.SelectedItem == null)
            {
                MessageBox.Show(this,"Select process to attach from the list","Message");
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void lbProcesses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Process pr = lbProcesses.SelectedItem as Process;

            if (pr != null)
            {
                try
                {
                    string str_module;
                    try
                    {
                        ProcessModule module;
                        module = pr.MainModule;
                        str_module = module.FileName;
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        str_module = "???";
                    }

                    tbInfo.Text = "PID: " + pr.Id.ToString() + "; Application: " + str_module;
                }
                catch (InvalidOperationException)
                {
                    tbInfo.Text = "???";
                }
            }
            else
            {
                tbInfo.Text = "";
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (this.processes != null)
            {
                for (int i = 0; i < processes.Length; i++)
                {
                    if (this.DialogResult == true && i == lbProcesses.SelectedIndex) continue;

                    this.processes[i].Dispose();
                }
            }
        }
    }
}
