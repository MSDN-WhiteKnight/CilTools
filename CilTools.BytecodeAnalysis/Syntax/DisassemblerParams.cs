/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.SourceCode;

namespace CilTools.Syntax
{
    /// <summary>
    /// Contains parameters that control how the disassembling operation works
    /// </summary>
    public class DisassemblerParams
    {
        /// <summary>
        /// Gets or sets an object used to load source code corresponding to disassembled bytecode. 
        /// Could be null is <see cref="IncludeSourceCode"/> property is set to false.
        /// </summary>
        public SourceCodeProvider CodeProvider { get; set; }

        /// <summary>
        /// Gets or sets a value specifying that disassembler should include source code in its output. 
        /// The source code is added as a comment before the CIL fragment it is corresponding to.
        /// </summary>
        /// <remarks>
        /// The <see cref="CodeProvider"/> property should be set to a valid <see cref="SourceCodeProvider"/> implementation 
        /// if this property is set to true.
        /// </remarks>
        public bool IncludeSourceCode { get; set; }

        internal static readonly DisassemblerParams Default = new DisassemblerParams();
    }
}
