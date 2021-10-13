/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using CilView.Build;

namespace CilView.UI.Dialogs
{
    public partial class OpenCodeWindow : Window
    {
        LangItem[] langs;

        public string Code 
        {
            get { return this.tbContent.Text; }
        }

        public CodeLanguage SelectedLanguage
        {
            get 
            {
                LangItem item = cbLang.SelectedItem as LangItem;

                if (item == null) return default;
                else return item.Language;
            }
        }

        public OpenCodeWindow()
        {
            InitializeComponent();

            langs = new LangItem[] { 
                new LangItem() { Name = "C#", Language = CodeLanguage.CSharp },
                new LangItem() { Name = "Visual Basic", Language = CodeLanguage.VB },
            };

            this.cbLang.ItemsSource = langs;
            this.cbLang.SelectedIndex = 0;

            const string initialContent = @"using System;
using System.Collections.Generic;
using System.Text;

public class Program
{
    public static void Main() 
    {
        Console.WriteLine(""Hello, world!"");
    }
}";
            tbContent.Text = initialContent;
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void bOK_Click(object sender, RoutedEventArgs e)
        {
            if (tbContent.Text.Trim().Length == 0) 
            {
                MessageBox.Show(this, "Enter code to open into the field above", "Error");
                return;
            }

            this.DialogResult = true;
            this.Close();
        }
    }
}
