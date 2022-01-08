/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CilTools.Syntax;

namespace CilView.Core.Syntax
{
    public static class SyntaxWriter
    {
        static async Task WriteSyntaxAsync(IEnumerable<SyntaxNode> nodes, TextWriter target)
        {
            foreach (SyntaxNode node in nodes)
            {
                string s = node.ToString();
                await target.WriteAsync(s);
            }
        }

        static async Task WriteHeaderAsync(TextWriter target)
        {
            Version ver = typeof(SyntaxWriter).Assembly.GetName().Version;
            await target.WriteLineAsync("// CIL Tools " + ver.ToString());
            await target.WriteLineAsync("// https://github.com/MSDN-WhiteKnight/CilTools");
            await target.WriteLineAsync();
        }

        public static async Task DisassembleTypeAsync(Type t, DisassemblerParams pars, TextWriter target)
        {
            await WriteHeaderAsync(target);
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, true, pars);
            await WriteSyntaxAsync(nodes, target);
            await target.FlushAsync();
        }

        public static async Task WriteContentAsync(string content, TextWriter target)
        {
            await WriteHeaderAsync(target);
            await target.WriteAsync(content);
            await target.FlushAsync();
        }
    }
}
