/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CilView.Build;
using Microsoft.Win32;

namespace CilView.UI.Dialogs
{
    public partial class OpenCodeWindow : Window
    {
        const string contextHelp = 
            "This dialog enables you to view disassembled CIL code corresponding to the entered code snippet. " +
            "It only supports a self-contained code snippet without any dependencies or special build options; open an " +
            "MSBuild project file (.csproj/.vbproj) instead if you need more complex scenarios.\r\n\r\n" +
            "CIL View will compile the provided code in background, and open the output binary if the " +
            "compilation is successful. " +
            "The code is compiled as a class library in DEBUG mode, with optimizations off. " +
            "Unsafe code is allowed in C#.";

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

        private void cbLang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.SelectedLanguage) 
            {
                case CodeLanguage.CSharp:
                    tbVersionInfo.Text = "Supports C# 5.0 targeting .NET Framework 4.5";
                    break;
                case CodeLanguage.VB:
                    tbVersionInfo.Text = "Supports VB.NET 11.0 targeting .NET Framework 4.5";
                    break;
            }
        }

        private void labelHelp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            TextViewWindow wnd = new TextViewWindow();
            wnd.Owner = this;
            wnd.Title = "About Open code dialog";
            wnd.Text = contextHelp;
            wnd.ShowDialog();
        }

        private void bInsert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();

                    if (string.IsNullOrWhiteSpace(text)) return;
                    
                    tbContent.SelectedText = text;
                    tbContent.Focus();
                }
            }
            catch (Exception ex) 
            {
                ErrorHandler.Current.Error(ex);
            }
        }

        private void bFromFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.RestoreDirectory = true;
                dlg.Filter = "Code files (*.cs,*.vb)|*.cs;*.vb|All files|*";

                if (dlg.ShowDialog(this) != true) return;

                string file = dlg.FileName;
                string text = File.ReadAllText(file);

                if (!string.IsNullOrWhiteSpace(text))
                {
                    tbContent.Text = text;
                }
                else
                {
                    MessageBox.Show(this, "File is empty", "Error");
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }
    }
}
