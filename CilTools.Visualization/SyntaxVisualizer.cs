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
    public abstract class SyntaxVisualizer
    {
        public abstract void RenderNode(SyntaxNode node, VisualizationOptions options, TextWriter target);
    }
}
