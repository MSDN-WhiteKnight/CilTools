/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace CilView.UI.Dialogs
{
    /// <summary>
    /// HtmlViewWindow codebehind
    /// </summary>
    public partial class HtmlViewWindow : Window
    {
        public HtmlViewWindow(string content)
        {
            InitializeComponent();
            this.content.NavigateToString(content);
        }

        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
