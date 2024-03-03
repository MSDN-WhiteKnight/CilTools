/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CilTools.Syntax;

namespace CilTools.Visualization
{
    /// <summary>
    /// Provides a base class for classes that visualize syntax nodes and output results using a <see cref="TextWriter"/>
    /// </summary>
    public abstract class SyntaxVisualizer
    {
        /// <summary>
        /// Visualizes the specified <see cref="SyntaxNode"/> and writes results into the <c>TextWriter</c>
        /// </summary>
        public abstract void RenderNode(SyntaxNode node, VisualizationOptions options, TextWriter target);

        /// <summary>
        /// Visualizes a paragraph with the specified text and writes results into the <c>TextWriter</c>
        /// </summary>
        public abstract void RenderParagraph(string content, TextWriter target);

        /// <summary>
        /// Writes content that marks the beginning of the visualized code block to the <c>TextWriter</c>
        /// </summary>
        protected abstract void StartBlock(VisualizationOptions options, TextWriter target);

        /// <summary>
        /// Writes content that marks the end of the visualized code block to the <c>TextWriter</c>
        /// </summary>
        protected abstract void EndBlock(VisualizationOptions options, TextWriter target);

        /// <summary>
        /// Visualizes the specified collection of syntax nodes and writes results into the <c>TextWriter</c>
        /// </summary>
        /// <param name="nodes">Collection of nodes to visualize</param>
        /// <param name="options">Options that control visualization output</param>
        /// <param name="target"><c>TextWriter</c> where to write output</param>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is null</exception>
        public void RenderNodes(IEnumerable<SyntaxNode> nodes, VisualizationOptions options, TextWriter target)
        {
            if (nodes == null) return;
            if (target == null) throw new ArgumentNullException("target");

            this.StartBlock(options, target);

            foreach (SyntaxNode node in nodes) this.RenderNode(node, options, target);

            this.EndBlock(options, target);
            target.Flush();
        }

        /// <summary>
        /// Visualizes the specified collection of syntax nodes and returns the resulting string
        /// </summary>
        /// <param name="nodes">Collection of nodes to visualize</param>
        /// <param name="options">Options that control visualization output</param>
        public string RenderToString(IEnumerable<SyntaxNode> nodes, VisualizationOptions options)
        {
            if (nodes == null) return string.Empty;

            StringBuilder sb = new StringBuilder();
            StringWriter wr = new StringWriter(sb);
            this.RenderNodes(nodes, options, wr);
            return sb.ToString();
        }

        /// <summary>
        /// Visualizes the specified collection of syntax nodes using default visualization options and returns the 
        /// resulting string
        /// </summary>
        /// <param name="nodes">Collection of nodes to visualize</param>
        public string RenderToString(IEnumerable<SyntaxNode> nodes)
        {
            return this.RenderToString(nodes, new VisualizationOptions());
        }

        public static SyntaxVisualizer Create(OutputFormat fmt)
        {
            switch (fmt)
            {
                case OutputFormat.Html: return new HtmlVisualizer();
                case OutputFormat.ConsoleText: return ConsoleVisualizer.Instance;
                default: return PlaintextVisualizer.Instance;
            }
        }
    }
}
