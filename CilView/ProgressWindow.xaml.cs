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
            op.Window = this;
            this.operation = op;
            this.progress.IsIndeterminate = true;
        }

        public OperationBase Operation { get { return this.operation; } }

        public string Text
        {
            get { return this.textblock.Text; }
            set { this.textblock.Text = value; }
        }

        void Cancel()
        {
            this.progress.IsIndeterminate = false;
            operation.Stop();
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            Cancel();
            this.Close();
        }

        public void ReportProgress(string txt,double curr,double max)
        {
            this.Dispatcher.Invoke(() => { 

                this.textblock.Text = txt;

                if (max > 0.0)
                {
                    this.progress.IsIndeterminate = false;
                    this.progress.Maximum = max;
                    this.progress.Value = curr;
                }
                else this.progress.IsIndeterminate = true;

            });
        }

        private async void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            try
            {
                await Task.Yield();

                await operation.Start();
                
                if (operation.Stopped) return;

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                if (operation.Stopped) return;

                MessageBox.Show(
                    this,ex.GetType().ToString()+": "+ex.Message,"Error",MessageBoxButton.OK,MessageBoxImage.Error
                    );

                this.DialogResult = false;
                this.Close();
            }
        }

        private void Window_Closed_1(object sender, EventArgs e)
        {
            Cancel();
        }
    }
}
