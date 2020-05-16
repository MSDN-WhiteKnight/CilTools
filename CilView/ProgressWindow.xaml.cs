/* CIL Tools 
 * Copyright (c) 2020, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CilView
{
    /// <summary>
    /// ProgressWindow codebehind
    /// </summary>
    public partial class ProgressWindow : Window
    {
        OperationBase operation;

        public ProgressWindow(OperationBase op)
        {
            InitializeComponent();
            this.operation = op;
            this.progress.IsIndeterminate = true;
        }

        public OperationBase Operation { get { return this.operation; } }

        public string Text
        {
            get { return this.textblock.Text; }
            set { this.textblock.Text = value; }
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            this.progress.IsIndeterminate = false;
            operation.Stop();
            this.DialogResult = false;
            this.Close();
        }

        private async void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            try
            {
                await operation.Start();
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                this.DialogResult = false;
                this.Close();
            }
        }
    }
}
