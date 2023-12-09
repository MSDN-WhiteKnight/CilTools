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
    /// Provides a base class for classes that visualize syntax nodes and output results using a <see cref="TextWriter"/>
    /// </summary>
    public abstract class SyntaxVisualizer
    {
        /// <summary>
        /// Visualizes the specified <see cref="SyntaxNode"/> and writes results into the <c>TextWriter</c>
        /// </summary>
        public abstract void RenderNode(SyntaxNode node, VisualizationOptions options, TextWriter target);
    }
}
