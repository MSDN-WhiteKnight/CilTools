/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Syntax
{
    /// <summary>
    /// Represents the kind of keyword in the <see cref="KeywordSyntax"/> node
    /// </summary>
    public enum KeywordKind
    {
        /// <summary>
        /// Unclassifed
        /// </summary>
        Other = 0,

        /// <summary>
        /// The name of directive
        /// </summary>
        DirectiveName = 1,

        /// <summary>
        /// The name of instruction
        /// </summary>
        InstructionName = 2
    }
}
