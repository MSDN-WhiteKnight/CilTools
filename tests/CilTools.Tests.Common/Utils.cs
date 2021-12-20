/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CilTools.Syntax;

namespace CilTools.Tests.Common
{
    public static class Utils
    {
        /// <summary>
        /// Provides <see cref="BindingFlags"/> mask that matches all members 
        /// (public and non-public, static and instance)
        /// </summary>
        public static BindingFlags AllMembers()
        {
            return BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance;
        }

        public static string SyntaxToString(IEnumerable<SyntaxNode> nodes) 
        {
            StringBuilder sb = new StringBuilder();
            StringWriter wr = new StringWriter(sb);

            foreach (SyntaxNode node in nodes)
            {
                node.ToText(wr);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets current project configuration name (Debug or Release)
        /// </summary>
        public static string GetConfig() 
        {
#if DEBUG 
            return "Debug";
#else
            return "Release";
#endif
        }
    }
}
