/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;

namespace CilView.UI.Controls
{
    /// <summary>
    /// CilBrowserPage codebehind
    /// </summary>
    public partial class CilBrowserPage : Page
    {
        public CilBrowserPage(UIElement content, string contentText, string location)
        {
            InitializeComponent();
            this.tbCurrLocation.Text = location;
            this.tbMainContent.Text = contentText;
            gridContent.Children.Clear();
            gridContent.Children.Add(content);
        }

        public void SetContent(UIElement content, string contentText, string location)
        {
            this.tbCurrLocation.Text = location;
            this.tbMainContent.Text = contentText;
            gridContent.Children.Clear();
            gridContent.Children.Add(content);
        }
    }
}
