/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using CilTools.SourceCode.Common;
using CilTools.Visualization;
using CilView.Common;
using CilView.Visualization;

namespace CilView.SourceCode
{
    public static class SourceVisualization
    {
        public static string VisualizeAsHtml(IEnumerable<SourceToken> tokens, string header, string caption)
        {
            StringBuilder sb = new StringBuilder();
            HtmlBuilder html = new HtmlBuilder(sb);
            HtmlVisualizer vis = new HtmlVisualizer();
            
            if (!string.IsNullOrEmpty(header))
            {
                html.StartParagraph();
                html.WriteElement("i", header);
                html.EndParagraph();
            }

            string rendered = vis.RenderSyntaxNodes(tokens);
            html.WriteRaw(rendered);

            if (!string.IsNullOrEmpty(caption))
            {
                html.StartParagraph();
                html.WriteElement("i", caption);
                html.EndParagraph();
            }

            return VisualizationServer.PrepareContent(sb.ToString());
        }
    }
}
