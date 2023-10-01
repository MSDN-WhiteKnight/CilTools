/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.SourceCode.Common;
using CilTools.Syntax;
using CilTools.Syntax.Tokens;

namespace CilTools.Visualization.SyntaxModel
{
    internal static class SourceParser
    {
        static Dictionary<string, SyntaxFactory> s_map = new Dictionary<string, SyntaxFactory>();
        
        /// <summary>
        /// Registers a syntax provider for the specified source file extension. Extension should be with a leading dot 
        /// and in all lowercase letters.
        /// </summary>
        public static void RegisterProvider(string ext, SyntaxFactory provider)
        {
            s_map[ext] = provider;
        }
        
        public static SyntaxNode[] Parse(string content, string ext)
        {
            ext = ext.ToLower();

            if (Utils.StrEquals(ext, ".il") || Utils.StrEquals(ext, ".cil"))
            {
                return SyntaxReader.ReadAllNodes(content);
            }
            else
            {
                SyntaxFactory provider;
                
                if (s_map.TryGetValue(ext, out provider))
                {
                    // Registered syntax provider
                    return provider.CreateNode(content, string.Empty, string.Empty).GetChildNodes();
                }
                else
                {
                    // CIL Tools built-in syntax highlighting support
                    return SyntaxReader.ReadAllNodes(content, SourceCodeUtils.GetTokenDefinitions(ext),
                        SourceCodeUtils.GetFactory(ext));
                }
            }
        }
    }
}
