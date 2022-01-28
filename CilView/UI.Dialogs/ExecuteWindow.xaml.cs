﻿/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CilView.Core.Reflection;

namespace CilView.UI.Dialogs
{
    public partial class ExecuteWindow : Window
    {
        bool executePressed;

        public ExecuteWindow(MethodParameter[] pars)
        {
            InitializeComponent();
            this.dg.ItemsSource = pars;
            this.dg.AutoGenerateColumns = true;
        }

        public bool ExecutePressed
        {
            get { return this.executePressed; }
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void bExecute_Click(object sender, RoutedEventArgs e)
        {
            this.executePressed = true;
            this.Close();
        }
    }
}
