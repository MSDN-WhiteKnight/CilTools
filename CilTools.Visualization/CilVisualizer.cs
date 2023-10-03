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
using CilTools.Syntax;

namespace CilTools.Visualization
{
    public class CilVisualizer : SyntaxVisualizer
    {
        void VisualizeMethodImpl(MethodBase mb, HtmlBuilder target)
        {
            CilGraph gr = CilGraph.Create(mb);
            SyntaxNode[] nodes = new SyntaxNode[] { gr.ToSyntaxTree() };
            this.VisualizeSyntaxNodes(nodes, target);
        }

        /// <summary>
        /// Renders IL of the specified method as HTML with syntax highlighting and writes the resulting markup into 
        /// <c>TextWriter</c>
        /// </summary>
        public void RenderMethod(MethodBase method, TextWriter target)
        {
            if (method == null) throw new ArgumentNullException("method");
            if (target == null) throw new ArgumentNullException("target");

            HtmlBuilder builder = new HtmlBuilder(target);
            this.VisualizeMethodImpl(method, builder);
            target.Flush();
        }

        /// <summary>
        /// Renders IL of the specified method as HTML with syntax highlighting and returns the string with resulting markup
        /// </summary>
        public string RenderMethod(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException("method");

            StringBuilder sb = new StringBuilder(5000);
            HtmlBuilder builder = new HtmlBuilder(sb);
            this.VisualizeMethodImpl(method, builder);
            return sb.ToString();
        }

        void VisualizeTypeImpl(Type t, bool full, HtmlBuilder target)
        {
            SyntaxNode[] nodes = SyntaxNode.GetTypeDefSyntax(t, full, new DisassemblerParams()).ToArray();

            if (nodes.Length == 0) return;

            if (nodes.Length == 1)
            {
                if (string.IsNullOrWhiteSpace(nodes[0].ToString())) return;
            }

            this.VisualizeSyntaxNodes(nodes, target);
        }

        /// <summary>
        /// Renders IL of the specified type as HTML with syntax highlighting and writes the resulting markup into 
        /// <c>TextWriter</c>
        /// </summary>
        public void RenderType(Type t, bool full, TextWriter target)
        {
            if (t == null) throw new ArgumentNullException("t");
            if (target == null) throw new ArgumentNullException("target");

            HtmlBuilder html = new HtmlBuilder(target);
            this.VisualizeTypeImpl(t, full, html);
            target.Flush();
        }

        /// <summary>
        /// Renders IL of the specified type as HTML with syntax highlighting and returns string with resulting markup
        /// </summary>
        public string RenderType(Type t, bool full)
        {
            if (t == null) throw new ArgumentNullException("t");

            StringBuilder sb = new StringBuilder();
            HtmlBuilder html = new HtmlBuilder(sb);
            this.VisualizeTypeImpl(t, full, html);
            return sb.ToString();
        }

        void VisualizeAssemblyManifestImpl(Assembly ass, HtmlBuilder target)
        {
            IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);
            this.VisualizeSyntaxNodes(nodes, target);
        }

        /// <summary>
        /// Renders IL of the specified assembly's manifest as HTML with syntax highlighting and writes the resulting 
        /// markup into <c>TextWriter</c>
        /// </summary>
        /// <exception cref="ArgumentNullException">Assembly or target <c>TextWriter</c> is null</exception>
        public void RenderAssemblyManifest(Assembly ass, TextWriter target)
        {
            if (ass == null) throw new ArgumentNullException("ass");
            if (target == null) throw new ArgumentNullException("target");

            HtmlBuilder html = new HtmlBuilder(target);
            this.VisualizeAssemblyManifestImpl(ass, html);
            target.Flush();
        }

        /// <summary>
        /// Renders IL of the specified assembly's manifest as HTML with syntax highlighting and returns string with 
        /// resulting markup
        /// </summary>
        /// <exception cref="ArgumentNullException">Assembly is null</exception>
        public string RenderAssemblyManifest(Assembly ass)
        {
            if (ass == null) throw new ArgumentNullException("ass");

            StringBuilder sb = new StringBuilder();
            HtmlBuilder html = new HtmlBuilder(sb);
            this.VisualizeAssemblyManifestImpl(ass, html);
            return sb.ToString();
        }
    }
}
