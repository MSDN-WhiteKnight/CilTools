/* CIL Tools 
 * Copyright (c) 2024,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Syntax;
using CilTools.Metadata;
using CilTools.Visualization;
using Html.Forms;

namespace CilView.UI
{
    class IndexPage : Page
    {
        public IndexPage()
        {
            this.FileName = "index.html";
            this.Html = ReadFromResource(this.FileName);
        }

        public string AssemblyPath { get; set; }

        protected override void OnLoad(LoadEventArgs args)
        {
            if (string.IsNullOrEmpty(this.AssemblyPath)) return;

            AssemblyReader reader = new AssemblyReader();
            HtmlVisualizer vis = new HtmlVisualizer();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(this.AssemblyPath);
                IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);
                string html = vis.RenderToString(nodes);
                this.SetField("result-html", html);
            }
        }
    }
}
