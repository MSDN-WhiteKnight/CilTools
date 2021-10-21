/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using CilView.Symbols;

namespace CilView.UI.Dialogs
{
    public partial class SourceViewWindow : Window
    {
        public SourceViewWindow(SourceInfo info)
        {
            InitializeComponent();
            tbMethod.Text = CilVisualization.MethodToString(info.Method);
            tbOffset.Text = info.CilStart.ToString() + "-" + info.CilEnd.ToString();

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
    }
}
