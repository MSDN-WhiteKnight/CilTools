/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
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

        public FontFamily TextFontFamily
        {
            get { return this.tbContent.FontFamily; }
            set { this.tbContent.FontFamily = value; }
        }

        public double TextFontSize
        {
            get { return this.tbContent.FontSize; }
            set { this.tbContent.FontSize = value; }
        }

        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
