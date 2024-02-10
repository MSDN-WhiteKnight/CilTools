/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CilTools.BytecodeAnalysis;
using CilTools.Syntax;
using CilTools.Visualization;
using CilView.Common;

namespace CilView.Core.Syntax
{
    public static class SyntaxWriter
    {
        static readonly string header = GenerateHeader();

        static async Task WriteSyntaxAsync(IEnumerable<SyntaxNode> nodes, TextWriter target)
        {
            foreach (SyntaxNode node in nodes)
            {
                string s = node.ToString();
                await target.WriteAsync(s);
            }
        }

        static string GenerateHeader()
        {
            Version ver = typeof(SyntaxWriter).Assembly.GetName().Version;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// CIL Tools " + ver.ToString());
            sb.AppendLine("// https://github.com/MSDN-WhiteKnight/CilTools");
            return sb.ToString();
        }

        public static async Task WriteHeaderAsync(TextWriter target)
        {
            await target.WriteLineAsync(header);
        }

        public static void WriteHeader(TextWriter target)
        {
            target.WriteLine(header);
        }

        static string GenerateDocumentStart()
        {
            Version ver = typeof(SyntaxWriter).Assembly.GetName().Version;
            StringBuilder sb = new StringBuilder();
            HtmlBuilder html = new HtmlBuilder(sb);
            html.WriteOpeningTag("html");
            html.WriteOpeningTag("head");
            html.WriteOpeningTag("style");
            html.WriteRaw(HtmlVisualizer.GetVisualStyles());
            html.WriteClosingTag("style");
            html.WriteClosingTag("head");
            html.StartParagraph();
            HtmlAttribute[] attrs = HtmlBuilder.OneAttribute("href", "https://gitflic.ru/project/smallsoft/ciltools");
            html.WriteElement("a", "CIL Tools " + ver.ToString(3), attrs);
            html.EndParagraph();
            html.WriteOpeningTag("body");
            return sb.ToString();
        }

        static async Task WriteDocumentStartAsync(TextWriter target)
        {
            await target.WriteLineAsync(GenerateDocumentStart());
        }

        static async Task WriteDocumentEndAsync(TextWriter target)
        {
            await target.WriteLineAsync("</body></html>");
        }

        public static void WriteDocumentStart(TextWriter target)
        {
            target.WriteLine(GenerateDocumentStart());
        }

        public static void WriteDocumentEnd(TextWriter target)
        {
            target.WriteLine("</body></html>");
        }
        
        public static async Task DisassembleAsync(Assembly ass, DisassemblerParams pars, OutputFormat fmt, TextWriter target)
        {
            SyntaxVisualizer vis = SyntaxVisualizer.Create(fmt);

            if (fmt == OutputFormat.Html)
            {
                await WriteDocumentStartAsync(target);
            }
            else
            {
                await WriteHeaderAsync(target);
            }
            
            //assembly manifest
            IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);
            string content = vis.RenderToString(nodes);
            await target.WriteLineAsync(content);

            //types
            Type[] types = ass.GetTypes();

            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].IsNested) continue;

                try
                {
                    nodes = SyntaxNode.GetTypeDefSyntax(types[i], true, pars).ToArray();
                }
                catch (NotSupportedException ex)
                {
                    //don't error out the whole loop if the current type contains unsupported elements
                    string commentStr = "// Failed to disassemble type " + types[i].FullName + ". "
                        + ex.GetType().ToString() + ": " + ex.Message;
                    SyntaxNode cs = SyntaxFactory.CreateFromToken(commentStr, string.Empty, Environment.NewLine);
                    nodes = new SyntaxNode[] { cs };
                }

                content = vis.RenderToString(nodes);
                await target.WriteLineAsync(content);
            }

            if (fmt == OutputFormat.Html) await WriteDocumentEndAsync(target);

            await target.FlushAsync();
        }

        public static async Task DisassembleTypeAsync(Type t, DisassemblerParams pars, OutputFormat fmt, TextWriter target)
        {
            SyntaxVisualizer vis = SyntaxVisualizer.Create(fmt);

            if (fmt == OutputFormat.Html)
            {
                await WriteDocumentStartAsync(target);
            }
            else
            {
                await WriteHeaderAsync(target);
            }

            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, true, pars);
            string content = vis.RenderToString(nodes);
            await target.WriteAsync(content);

            if (fmt == OutputFormat.Html) await WriteDocumentEndAsync(target);

            await target.FlushAsync();
        }

        public static void DisassembleMethod(MethodBase m, DisassemblerParams pars, OutputFormat fmt, TextWriter target)
        {
            //this is only used in CommandLine so don't need to be async
            CilGraph graph = CilGraph.Create(m);
            SyntaxNode root = graph.ToSyntaxTree(pars);

            SyntaxVisualizer vis = SyntaxVisualizer.Create(fmt);
            vis.RenderNodes(new SyntaxNode[] { root }, new VisualizationOptions(), target);
            target.Flush();
        }

        public static async Task WriteContentAsync(string content, TextWriter target, OutputFormat fmt)
        {
            if (fmt == OutputFormat.Html) await WriteDocumentStartAsync(target);
            else await WriteHeaderAsync(target);

            await target.WriteAsync(content);

            if (fmt == OutputFormat.Html) await WriteDocumentEndAsync(target);

            await target.FlushAsync();
        }
    }
}
