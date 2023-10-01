/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.Syntax;
using CilTools.Syntax.Tokens;

namespace CilTools.SourceCode.Common
{
    /// <summary>
    /// Provides static methods that convert source text into syntax nodes
    /// </summary>
    public static class SourceParser
    {
        static Dictionary<string, SyntaxFactory> s_map = new Dictionary<string, SyntaxFactory>();
        
        /// <summary>
        /// Registers a syntax factory for the specified source file extension. Extension should be with a leading dot 
        /// and in all lowercase letters.
        /// </summary>
        public static void RegisterSyntaxFactory(string ext, SyntaxFactory factory)
        {
            s_map[ext] = factory;
        }

        /// <summary>
        /// Converts the specified source text into a collection of parsed syntax nodes, using the language specified 
        /// by source file extension.
        /// </summary>
        /// <param name="sourceText">Source text to parse</param>
        /// <param name="ext">Source file extension with a leading dot (for example, <c>.cs</c> for C#)</param>
        public static SyntaxNodeCollection Parse(string sourceText, string ext)
        {
            ext = ext.ToLower();

            if (string.Equals(ext, ".il", StringComparison.Ordinal) || 
                string.Equals(ext, ".cil", StringComparison.Ordinal))
            {
                return SyntaxNodeCollection.FromArray(SyntaxReader.ReadAllNodes(sourceText));
            }
            else
            {
                SyntaxFactory factory;
                
                if (s_map.TryGetValue(ext, out factory))
                {
                    // Registered syntax factory
                    SyntaxNode root = factory.CreateNode(sourceText, string.Empty, string.Empty);

                    if (root is SyntaxNodeCollection) return (SyntaxNodeCollection)root;
                    else return SyntaxNodeCollection.FromArray(root.GetChildNodes());
                }
                else
                {
                    // CIL Tools built-in syntax highlighting support
                    SyntaxNode[] nodes = SyntaxReader.ReadAllNodes(sourceText, SourceCodeUtils.GetTokenDefinitions(ext),
                        SourceCodeUtils.GetFactory(ext));

                    return SyntaxNodeCollection.FromArray(nodes);
                }
            }
        }
    }
}
