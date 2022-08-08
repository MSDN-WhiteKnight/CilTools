/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CilView.SourceCode
{
    public static class SourceVisualization
    {
        static void VisualizeToken(SourceToken token, Paragraph target)
        {
            Run r;
            StringBuilder sb = new StringBuilder(500);
            StringWriter wr = new StringWriter(sb);
            r = new Run();

            switch (token.Kind)
            {
                case SourceTokenKind.Keyword: r.Foreground = Brushes.Blue;break;
                case SourceTokenKind.TypeName: r.Foreground = CilVisualization.IdentifierBrush; break;
                case SourceTokenKind.StringLiteral: r.Foreground = Brushes.Red;break;
                case SourceTokenKind.Comment: r.Foreground = Brushes.Green; break;
            }

            r.Text = token.ToString();
            target.Inlines.Add(r);
        }

        public static FlowDocument VisualizeTokens(IEnumerable<SourceToken> tokens, string header, string caption)
        {
            FlowDocument fd = new FlowDocument();
            fd.TextAlignment = TextAlignment.Left;
            fd.FontFamily = new FontFamily("Courier New");
            Paragraph par;

            if (!string.IsNullOrEmpty(header))
            {
                par = new Paragraph();
                Run r = new Run(header);
                r.FontFamily = SystemFonts.MessageFontFamily;
                r.FontStyle = FontStyles.Italic;
                par.Inlines.Add(r);
                fd.Blocks.Add(par);
            }

            par = new Paragraph();

            foreach (SourceToken token in tokens)
            {
                VisualizeToken(token, par);
            }
            
            fd.Blocks.Add(par);

            if (!string.IsNullOrEmpty(caption))
            {
                par = new Paragraph();
                Run r = new Run(caption);
                r.FontFamily = SystemFonts.MessageFontFamily;
                r.FontStyle = FontStyles.Italic;
                par.Inlines.Add(r);
                fd.Blocks.Add(par);
            }

            return fd;
        }
    }
}
