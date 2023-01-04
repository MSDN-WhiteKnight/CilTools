/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CilTools.Syntax.Tokens
{
    /// <summary>
    /// Reads tokens from a string and transforms them into corresponding <see cref="SyntaxNode"/> instances
    /// </summary>
    public static class SyntaxReader
    {
        static bool IsWhitespace(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (!char.IsWhiteSpace(str[i])) return false;
            }

            return true;
        }

        /// <summary>
        /// Reads all tokens from the specified string using the specified collection of token definitions and syntax factory
        /// </summary>
        /// <param name="src">Input string</param>
        /// <param name="tokenDefinitions">
        /// Collection of token definitions that will be used to split the input string into a sequence of tokens
        /// </param>
        /// <param name="factory">
        /// Syntax factory object that will be used to create new <see cref="SyntaxNode"/> instances
        /// </param>
        /// <returns>Array of syntax nodes that contain tokens</returns>
        public static SyntaxNode[] ReadAllNodes(string src, IEnumerable<SyntaxTokenDefinition> tokenDefinitions, 
            SyntaxFactory factory)
        {
            List<SyntaxNode> nodes = new List<SyntaxNode>();
            TokenReader reader = new TokenReader(src, tokenDefinitions);
            string[] tokens = reader.ReadAll().ToArray();
            if (tokens.Length == 0) return SyntaxNode.EmptyArray;

            string leadingWhitespace;
            int i = 0;

            if (IsWhitespace(tokens[0]))
            {
                leadingWhitespace = tokens[0];
                i = 1;
            }
            else
            {
                leadingWhitespace = string.Empty;
            }

            while (true)
            {
                if (i >= tokens.Length) break;

                if (i + 1 >= tokens.Length)
                {
                    nodes.Add(factory.CreateNode(tokens[i], leadingWhitespace, string.Empty));
                    break;
                }
                else if (IsWhitespace(tokens[i + 1]))
                {
                    nodes.Add(factory.CreateNode(tokens[i], leadingWhitespace, tokens[i + 1]));
                    i += 2;
                }
                else
                {
                    nodes.Add(factory.CreateNode(tokens[i], leadingWhitespace, string.Empty));
                    i++;
                }

                leadingWhitespace = string.Empty;
            }

            return nodes.ToArray();
        }

        /// <summary>
        /// Reads all CIL assembler tokens from the specified string
        /// </summary>
        /// <param name="src">Input string</param>
        /// <returns>Array of syntax nodes that contain tokens</returns>
        public static SyntaxNode[] ReadAllNodes(string src)
        {
            return ReadAllNodes(src, SyntaxTokenDefinition.IlasmTokens, IlasmTokenFactory.Value);
        }
    }
}
