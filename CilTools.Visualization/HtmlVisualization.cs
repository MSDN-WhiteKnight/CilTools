/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.SourceCode.Common;
using CilTools.Syntax;

namespace CilTools.Visualization
{
    public static class HtmlVisualization
    {
        /// <summary>
        /// Visualizes the specified collection of syntax nodes as HTML and writes result into HtmlBuilder
        /// </summary>
        internal static void VisualizeSyntaxNodes(IEnumerable<SyntaxNode> nodes, SyntaxVisualizer visualizer, 
            VisualizationOptions options, HtmlBuilder target)
        {
            target.WriteOpeningTag("pre", HtmlBuilder.OneAttribute("style", "white-space: pre-wrap;"));
            target.WriteOpeningTag("code");

            //content
            foreach (SyntaxNode node in nodes) visualizer.RenderNode(node, options, target.Target);

            target.WriteClosingTag("code");
            target.WriteClosingTag("pre");
        }

        static void VisualizeMethodImpl(MethodBase mb, SyntaxVisualizer visualizer, 
            VisualizationOptions options, HtmlBuilder target)
        {
            CilGraph gr = CilGraph.Create(mb);
            SyntaxNode[] nodes = new SyntaxNode[] { gr.ToSyntaxTree() };
            VisualizeSyntaxNodes(nodes, visualizer, options, target);
        }

        /// <summary>
        /// Renders IL of the specified method as HTML with syntax highlighting and returns the string with resulting markup
        /// </summary>
        public static string RenderMethod(MethodBase method, SyntaxVisualizer visualizer, VisualizationOptions options)
        {
            if (method == null) throw new ArgumentNullException("method");

            StringBuilder sb = new StringBuilder(5000);
            HtmlBuilder builder = new HtmlBuilder(sb);
            VisualizeMethodImpl(method, visualizer, options, builder);
            return sb.ToString();
        }

        static void VisualizeTypeImpl(Type t, SyntaxVisualizer visualizer, bool full, HtmlBuilder target)
        {
            SyntaxNode[] nodes = SyntaxNode.GetTypeDefSyntax(t, full, new DisassemblerParams()).ToArray();

            if (nodes.Length == 0) return;

            if (nodes.Length == 1)
            {
                if (string.IsNullOrWhiteSpace(nodes[0].ToString())) return;
            }

            VisualizeSyntaxNodes(nodes, visualizer, new VisualizationOptions(), target);
        }

        /// <summary>
        /// Renders IL of the specified type as HTML with syntax highlighting and returns string with resulting markup
        /// </summary>
        public static string RenderType(Type t, SyntaxVisualizer visualizer, bool full)
        {
            if (t == null) throw new ArgumentNullException("t");

            StringBuilder sb = new StringBuilder();
            HtmlBuilder html = new HtmlBuilder(sb);
            VisualizeTypeImpl(t, visualizer, full, html);
            return sb.ToString();
        }

        static void VisualizeAssemblyManifestImpl(Assembly ass, SyntaxVisualizer visualizer, HtmlBuilder target)
        {
            IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);
            VisualizeSyntaxNodes(nodes, visualizer, new VisualizationOptions(), target);
        }

        /// <summary>
        /// Renders IL of the specified assembly's manifest as HTML with syntax highlighting and returns string with 
        /// resulting markup
        /// </summary>
        /// <exception cref="ArgumentNullException">Assembly is null</exception>
        public static string RenderAssemblyManifest(Assembly ass, SyntaxVisualizer visualizer)
        {
            if (ass == null) throw new ArgumentNullException("ass");

            StringBuilder sb = new StringBuilder();
            HtmlBuilder html = new HtmlBuilder(sb);
            VisualizeAssemblyManifestImpl(ass, visualizer, html);
            return sb.ToString();
        }

        static void VisualizeSourceTextImpl(string content, string ext, HtmlBuilder html)
        {
            //convert source text into tokens
            SyntaxNodeCollection coll = SourceParser.Parse(content, ext);

            //convert tokens to HTML
            HtmlVisualizer vis = new HtmlVisualizer();
            vis.VisualizeSyntaxNodes(coll.GetChildNodes(), new VisualizationOptions(), html);
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
            
            HtmlBuilder builder = new HtmlBuilder(target);
            VisualizeSourceTextImpl(sourceText, ext, builder);
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
            HtmlBuilder builder = new HtmlBuilder(wr);
            VisualizeSourceTextImpl(sourceText, ext, builder);
            return sb.ToString();
        }
    }
}
