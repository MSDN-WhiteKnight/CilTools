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
    /// Visualizes syntax nodes as plain text
    /// </summary>
    public class PlaintextVisualizer : SyntaxVisualizer
    {
        public override void RenderNode(SyntaxNode node, VisualizationOptions options, TextWriter target)
        {
            node.ToText(target);
        }

        protected override void EndBlock(VisualizationOptions options, TextWriter target) { }

        protected override void StartBlock(VisualizationOptions options, TextWriter target) { }
    }
}
