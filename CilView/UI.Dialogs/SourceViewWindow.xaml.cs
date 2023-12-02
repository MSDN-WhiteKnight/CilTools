/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using CilTools.SourceCode;
using CilTools.SourceCode.Common;
using CilTools.Syntax;
using CilTools.Visualization;
using CilView.Common;
using CilView.SourceCode;

namespace CilView.UI.Dialogs
{
    public partial class SourceViewWindow : Window
    {
        SourceFragment _f;

        void LoadSourceInfo(SourceFragment f)
        {
            this._f = f;

            StringBuilder method_str = new StringBuilder(200);
            method_str.Append(CilVisualization.MethodToString(f.Method));
            method_str.Append(", byte offset: ");
            method_str.Append(f.CilStart.ToString());
            method_str.Append('-');
            method_str.Append(f.CilEnd.ToString());
            tbMethod.Text = method_str.ToString();

            StringBuilder sb = new StringBuilder(500);
            sb.Append(f.Document.FilePath);
            sb.Append("; lines: ");
            sb.Append(f.LineStart);
            sb.Append("-");
            sb.Append(f.LineEnd);
            tbFileName.Text = sb.ToString();

            try
            {
                //get CIL for the range of the sequence point
                IEnumerable<SyntaxNode> nodes = PdbUtils.GetCilSyntax(f.Method, (uint)f.CilStart, (uint)f.CilEnd);

                //visualize nodes as HTML in WebBrowser
                VisualizationOptions options = new VisualizationOptions();
                options.EnableInstructionNavigation = false;
                UIElement elem = CilVisualization.VisualizeAsHtml(nodes, null, options);
                contentCIL.Content = elem;
            }
            catch (Exception ex)
            {
                //don't stop the rest of method to work if this errors out
                //showing source can still be useful
                TextBlock errorBlock = new TextBlock(new Run("[error!]"));
                errorBlock.TextAlignment = TextAlignment.Left;
                contentCIL.Content = errorBlock;
                ErrorHandler.Current.Error(ex);
            }

            //source text
            string ext = Path.GetExtension(f.Document.FilePath);

            SourceToken[] tokens = SourceCodeUtils.ReadAllTokens(f.Text, SourceCodeUtils.GetTokenDefinitions(ext),
                SourceCodeUtils.GetFactory(ext));

            fdSource.Document = SourceVisualization.VisualizeTokens(tokens, string.Empty, string.Empty);

            if (f.CilStart == 0) bPrevious.IsEnabled = false;
            else bPrevious.IsEnabled = true;

            int size = Utils.GetMethodBodySize(f.Method);
            if (f.CilEnd >= size) bNext.IsEnabled = false;
            else bNext.IsEnabled = true;

            sb.Clear();
            sb.Append("Symbols file: ");
            sb.Append(f.Document.SymbolsFile);
            sb.Append(" (");
            sb.Append(f.Document.SymbolsFileFormat);
            sb.Append(')');
            tbSymbolsFile.Text = sb.ToString();
        }

        public SourceViewWindow(SourceFragment f)
        {
            InitializeComponent();
            this.LoadSourceInfo(f);
        }

        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void tbFileName_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            //open the source file in external editor
            WpfUtils.ShellExecute(this._f.Document.FilePath, this, "Failed to open source code file");
        }
        
        private void bPrevious_Click(object sender, RoutedEventArgs e)
        {
            uint offset = (uint)this._f.CilStart;
            if (offset == 0) return;

            offset--;

            try
            {
                SourceFragment f_new = PdbUtils.FindFragment(this._f.Document.Fragments, offset);

                if (f_new == null)
                {
                    MessageBox.Show("Failed to get the previous sequence point");
                    return;
                }

                this.LoadSourceInfo(f_new);
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }

        private void bNext_Click(object sender, RoutedEventArgs e)
        {
            int offset = this._f.CilEnd;
            int size = Utils.GetMethodBodySize(this._f.Method);
            if (offset >= size) return;

            offset++;

            try
            {
                SourceFragment f_new = PdbUtils.FindFragment(this._f.Document.Fragments, (uint)offset);

                if (f_new == null)
                {
                    MessageBox.Show("Failed to get the next sequence point");
                    return;
                }

                this.LoadSourceInfo(f_new);
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }

        private void tbSymbolsFile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            SourceCodeUI.OpenSymbolsDir(this._f.Document.SymbolsFile, this);
        }
    }
}
