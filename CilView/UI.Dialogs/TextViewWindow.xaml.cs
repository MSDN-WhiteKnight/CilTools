/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CilView.UI.Dialogs
{
    /// <summary>
    /// TextViewWindow codebehind
    /// </summary>
    public partial class TextViewWindow : Window
    {
        public TextViewWindow()
        {
            InitializeComponent();
        }

        public string Text
        {
            get { return this.tbContent.Text; }
            set { this.tbContent.Text = value; }
        }

        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
