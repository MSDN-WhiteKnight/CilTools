/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CilTools.BytecodeAnalysis;
using CilTools.Syntax;
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

        static async Task WriteHeaderAsync(TextWriter target)
        {
            await target.WriteLineAsync(header);
        }

        public static void WriteHeader(TextWriter target)
        {
            target.WriteLine(header);
        }

        public static async Task DisassembleAsync(Assembly ass, DisassemblerParams pars, TextWriter target)
        {
            await WriteHeaderAsync(target);

            //assembly manifest
            IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);
            await WriteSyntaxAsync(nodes, target);
            await target.WriteLineAsync();

            //types
            Type[] types = ass.GetTypes();

            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].IsNested) continue;
                if (Utils.StringEquals(types[i].FullName, "<Module>")) continue;

                nodes = SyntaxNode.GetTypeDefSyntax(types[i], true, pars);
                await WriteSyntaxAsync(nodes, target);
                await target.WriteLineAsync();
            }

            await target.FlushAsync();
        }

        public static async Task DisassembleTypeAsync(Type t, DisassemblerParams pars, TextWriter target)
        {
            await WriteHeaderAsync(target);
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, true, pars);
            await WriteSyntaxAsync(nodes, target);
            await target.FlushAsync();
        }

        public static void DisassembleMethod(MethodBase m, DisassemblerParams pars, TextWriter target)
        {
            //this is only used in CommandLine so don't need to be async
            CilGraph graph = CilGraph.Create(m);
            SyntaxNode root = graph.ToSyntaxTree(pars);
            root.ToText(target);
            target.Flush();
        }

        public static async Task WriteContentAsync(string content, TextWriter target)
        {
            await WriteHeaderAsync(target);
            await target.WriteAsync(content);
            await target.FlushAsync();
        }
    }
}
