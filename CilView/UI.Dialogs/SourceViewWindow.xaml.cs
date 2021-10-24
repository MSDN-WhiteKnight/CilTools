/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CilView.Common;
using CilView.SourceCode;

namespace CilView.UI.Dialogs
{
    public partial class SourceViewWindow : Window
    {
        SourceInfo _info;

        public SourceViewWindow(SourceInfo info)
        {
            InitializeComponent();
            this._info = info;

            StringBuilder method_str = new StringBuilder(200);
            method_str.Append(CilVisualization.MethodToString(info.Method));
            method_str.Append(", byte offset: ");
            method_str.Append(info.CilStart.ToString());
            method_str.Append('-');
            method_str.Append(info.CilEnd.ToString());
            tbMethod.Text = method_str.ToString();

            StringBuilder sb = new StringBuilder(500);
            sb.Append(info.SourceFile);
            sb.Append("; lines: ");
            sb.Append(info.LineStart);
            sb.Append("-");
            sb.Append(info.LineEnd);
            tbFileName.Text = sb.ToString();

            try
            {
                //get CIL for the range of the sequence point
                tbCIL.Text=PdbUtils.GetCilText(info.Method, info.CilStart, info.CilEnd);
            }
            catch (Exception ex)
            {
                //don't stop the rest of method to work if this errors out
                //showing source can still be useful
                tbCIL.Text = "[error!]";
                ErrorHandler.Current.Error(ex);
            }

            tbSource.Text = info.SourceCode;
        }

        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void tbFileName_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            //open the source file in external editor
            Utils.ShellExecute(this._info.SourceFile, this, 
                "Failed to open source code file");
        }
    }
}
