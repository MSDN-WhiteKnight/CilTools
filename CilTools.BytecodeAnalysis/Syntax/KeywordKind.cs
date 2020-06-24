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
        Other = 0,
        DirectiveName = 1,
        InstructionName = 2
    }
}
