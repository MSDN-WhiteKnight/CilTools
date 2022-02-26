/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CilView.Core;
using CilView.Core.Reflection;

namespace CilView.UI.Dialogs
{
    public partial class ExecuteWindow : Window
    {
        bool executePressed;
        int timeoutMilliseconds=2000;

        public ExecuteWindow(MethodParameter[] pars)
        {
            InitializeComponent();
            this.dg.ItemsSource = pars;
            this.dg.AutoGenerateColumns = true;
        }

        public static void ShowExecuteMethodUI(MethodBase mb, Window owner)
        {
            try
            {
                string errorMessage;

                if (!MethodRunner.CanExecute(mb, out errorMessage))
                {
                    MessageBox.Show(owner, "Cannot execute method: " + errorMessage, "Error");
                    return;
                }

                MethodBase mbRuntime = MethodRunner.GetRuntimeMethod(mb);

                if (mbRuntime == null)
                {
                    MessageBox.Show(owner, "Cannot find runtime method", "Error");
                    return;
                }

                MethodParameter[] pars = MethodRunner.GetMethodParameters(mbRuntime);
                ExecuteWindow wnd = new ExecuteWindow(pars);
                wnd.Owner = owner;
                wnd.ShowDialog();

                if (!wnd.ExecutePressed) return;

                MethodExecutionResults res = MethodRunner.ExecuteMethod(mbRuntime, pars,
                    TimeSpan.FromMilliseconds(wnd.TimeoutMilliseconds));

                TextViewWindow resultsWindow = new TextViewWindow();
                resultsWindow.Owner = owner;
                resultsWindow.Title = "Method execution results";
                resultsWindow.Text = res.GetText();
                resultsWindow.Show();
            }
            catch (NotSupportedException ex)
            {
                //don't pollute logs with expected failures
                MessageBox.Show(owner, "Cannot execute method: " + ex.Message, "Error");
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }

        public bool ExecutePressed
        {
            get { return this.executePressed; }
        }

        public int TimeoutMilliseconds
        {
            get { return this.timeoutMilliseconds; }
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void bExecute_Click(object sender, RoutedEventArgs e)
        {
            int t;

            if (!int.TryParse(this.tbTimeout.Text, out t))
            {
                MessageBox.Show(this, "Timeout value must be an integer number", "Error");
                return;
            }

            if (t <= 0)
            {
                MessageBox.Show(this, "Timeout value must be positive", "Error");
                return;
            }

            this.timeoutMilliseconds = t;
            this.executePressed = true;
            this.Close();
        }
    }
}
