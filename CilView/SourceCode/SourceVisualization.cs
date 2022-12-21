/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CilTools.Syntax;

namespace CilView.SourceCode
{
    public static class SourceVisualization
    {
        static void VisualizeToken(SourceToken token, Paragraph target)
        {
            Run r;
            r = new Run();

            switch (token.Kind)
            {
                case TokenKind.Keyword: r.Foreground = Brushes.Blue;break;
                case TokenKind.TypeName: r.Foreground = CilVisualization.IdentifierBrush; break;
                case TokenKind.DoubleQuotLiteral: r.Foreground = Brushes.Red;break;
                case TokenKind.SingleQuotLiteral: r.Foreground = Brushes.Red; break;
                case TokenKind.Comment: r.Foreground = Brushes.Green; break;
                case TokenKind.MultilineComment: r.Foreground = Brushes.Green; break;
            }

            r.Text = token.ToString();
            target.Inlines.Add(r);
        }

        public static FlowDocument VisualizeTokens(IEnumerable<SourceToken> tokens, string header, string caption)
        {
            FlowDocument fd = CilVisualization.CreateFlowDocument();
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
