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
    /// Visualizes syntax node as a console text with syntax highlighting
    /// </summary>
    /// <remarks>
    /// NOTE: This visualizer should only be used to write output into a standard output stream attached to console.
    /// </remarks>
    class ConsoleVisualizer : SyntaxVisualizer
    {
        private ConsoleVisualizer() { }

        internal static readonly ConsoleVisualizer Instance = new ConsoleVisualizer();

        public override void RenderNode(SyntaxNode node, VisualizationOptions options, TextWriter target)
        {
            // Recursively prints CIL syntax tree to console

            SyntaxNode[] children = node.GetChildNodes();

            if (children.Length == 0) //if it a leaf node, print its content to console
            {
                //hightlight syntax elements
                ConsoleColor originalColor = Console.ForegroundColor;

                if (node is KeywordSyntax)
                {
                    KeywordSyntax ks = (KeywordSyntax)node;

                    if (ks.Kind == KeywordKind.Other) Console.ForegroundColor = ConsoleColor.Cyan;
                    else if (ks.Kind == KeywordKind.DirectiveName) Console.ForegroundColor = ConsoleColor.Magenta;
                }
                else if (node is IdentifierSyntax)
                {
                    IdentifierSyntax id = (IdentifierSyntax)node;

                    if (id.IsMemberName) Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else if (node is LiteralSyntax)
                {
                    LiteralSyntax lit = (LiteralSyntax)node;
                    if (lit.Value is string) Console.ForegroundColor = ConsoleColor.Red;
                }
                else if (node is CommentSyntax)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }

                node.ToText(target);

                //restore console color to default value
                Console.ForegroundColor = originalColor;
            }
            else //if the node has child nodes, process them
            {
                for (int i = 0; i < children.Length; i++)
                {
                    this.RenderNode(children[i], options, target);
                }
            }
        }

        protected override void EndBlock(VisualizationOptions options, TextWriter target) { }

        protected override void StartBlock(VisualizationOptions options, TextWriter target) { }
    }
}
