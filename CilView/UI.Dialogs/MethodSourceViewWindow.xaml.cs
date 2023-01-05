/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using CilTools.SourceCode;
using CilTools.SourceCode.Common;
using CilTools.Syntax;
using CilView.Common;
using CilView.SourceCode;

namespace CilView.UI.Dialogs
{
    /// <summary>
    /// MethodSourceViewWindow codebehind
    /// </summary>
    public partial class MethodSourceViewWindow : Window
    {
        SourceDocument _doc;

        public MethodSourceViewWindow()
        {
            InitializeComponent();
        }

        public void LoadDocument(SourceDocument doc)
        {
            this._doc = doc;
            string src = doc.Text;
            string ext = Path.GetExtension(doc.FilePath);
            StringBuilder sb;
            SourceToken[] sigTokens = new SourceToken[0];

            try
            {
                if (!Utils.IsConstructor(doc.Method) && !Utils.IsPropertyMethod(doc.Method))
                {
                    sigTokens = Decompiler.DecompileMethodSignature(ext, doc.Method).ToArray();
                }
            }
            catch (Exception ex)
            {
                //don't error out if we can't build good signature string
                ErrorHandler.Current.Error(ex, "PdbUtils.GetMethodSigString", silent: true);
                string methodstr = CilVisualization.MethodToString(doc.Method);
                sigTokens = new SourceToken[] { new SourceToken(methodstr, TokenKind.Unknown) };
            }

            //header
            sb = new StringBuilder();
            sb.Append(doc.FilePath);
            sb.AppendFormat(", lines {0}-{1}", doc.LineStart, doc.LineEnd);
            string header = sb.ToString();

            //body
            sb = new StringBuilder(src.Length + 2);
            string srcDeindented = PdbUtils.Deindent(src);
            sb.Append(srcDeindented);

            if (Decompiler.IsCppExtension(ext))
            {
                //C++ PDB sequence points don't include the trailing brace for some reason
                if (!srcDeindented.EndsWith(Environment.NewLine, StringComparison.Ordinal)) sb.AppendLine();
                sb.Append('}');
            }

            SourceToken[] bodyTokens = SourceCodeUtils.ReadAllTokens(sb.ToString(), SourceCodeUtils.GetTokenDefinitions(ext),
                SourceCodeUtils.GetFactory(ext));

            List<SourceToken> tokens = new List<SourceToken>(sigTokens.Length + bodyTokens.Length + 1);
            tokens.AddRange(sigTokens);
            tokens.Add(new SourceToken(Environment.NewLine, TokenKind.Unknown));
            tokens.AddRange(bodyTokens);

            //caption
            sb = new StringBuilder();
            sb.Append("Symbols file: ");
            sb.Append(doc.SymbolsFile);
            sb.Append(" (");
            sb.Append(doc.SymbolsFileFormat);
            sb.Append(')');
            string caption = sb.ToString();

            //show source code
            FlowDocument fd = SourceVisualization.VisualizeTokens(tokens, string.Empty, string.Empty);
            this.viewer.Document = fd;
            this.tbFileName.Text = header;
            this.tbSymbolsFile.Text = caption;
        }

        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void tbFileName_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            //open the source file in external editor
            WpfUtils.ShellExecute(this._doc.FilePath, this, "Failed to open source code file");
        }

        private void tbSymbolsFile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            SourceCodeUI.OpenSymbolsDir(this._doc.SymbolsFile, this);
        }
    }
}
