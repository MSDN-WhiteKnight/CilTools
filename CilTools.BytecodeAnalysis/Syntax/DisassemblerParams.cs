/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.SourceCode;

namespace CilTools.Syntax
{
    public class DisassemblerParams
    {
        public SourceCodeProvider CodeProvider { get; set; }
        public bool IncludeSourceCode { get; set; }

        internal static readonly DisassemblerParams Default = new DisassemblerParams();
    }
}
