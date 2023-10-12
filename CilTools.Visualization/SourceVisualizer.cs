/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CilTools.SourceCode.Common;

namespace CilTools.Visualization
{
    public class SourceVisualizer : SyntaxVisualizer
    {
        void VisualizeSourceTextImpl(string content, string ext, HtmlBuilder html)
        {
            //convert source text into tokens
            SyntaxNodeCollection coll = SourceParser.Parse(content, ext);

            //convert tokens to HTML
            this.VisualizeSyntaxNodes(coll.GetChildNodes(), new VisualizationOptions(), html);
        }

        /// <summary>
        /// Generates HTML markup with syntax highlighting for the specified source text and writes it into TextWriter. 
        /// </summary>
        /// <param name="sourceText">Source text to render</param>
        /// <param name="ext">
        /// Source file extension (with leading dot) that defines programming language. For example, <c>.cs</c> for C#.
        /// </param>
        /// <param name="target">Text writer where to output the resulting HTML</param>
        public static void RenderSourceText(string sourceText, string ext, TextWriter target)
        {
            if (string.IsNullOrEmpty(sourceText)) return;

            if (target == null) throw new ArgumentNullException("target");

            SourceVisualizer vis = new SourceVisualizer();
            HtmlBuilder builder = new HtmlBuilder(target);
            vis.VisualizeSourceTextImpl(sourceText, ext, builder);
            target.Flush();
        }

        /// <summary>
        /// Generates HTML markup with syntax highlighting for the specified source text.
        /// </summary>
        /// <param name="sourceText">Source text to render</param>
        /// <param name="ext">
        /// Source file extension (with leading dot) that defines programming language. For example, <c>.cs</c> for C#.
        /// </param>
        public static string RenderSourceText(string sourceText, string ext)
        {
            if (string.IsNullOrEmpty(sourceText)) return string.Empty;

            StringBuilder sb = new StringBuilder(sourceText.Length * 2);
            StringWriter wr = new StringWriter(sb);
            SourceVisualizer vis = new SourceVisualizer();
            HtmlBuilder builder = new HtmlBuilder(wr);
            vis.VisualizeSourceTextImpl(sourceText, ext, builder);
            return sb.ToString();
        }
    }
}
