/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace CilView.UI.Dialogs
{
    /// <summary>
    /// DocumentViewWindow.xaml codebehind
    /// </summary>
    public partial class DocumentViewWindow : Window
    {
        public DocumentViewWindow()
        {
            InitializeComponent();
        }

        public FlowDocument Document
        {
            get { return this.viewer.Document; }
            set { this.viewer.Document = value; }
        }

        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
