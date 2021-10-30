/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilView.SourceCode
{
    /// <summary>
    /// Contains information about a source code fragment corresponding to some bytecode fragment 
    /// (or whole method body)
    /// </summary>
    public class SourceInfo
    {
        public SourceInfo() 
        {
            this.SourceCode = string.Empty;
            this.SourceFile = string.Empty;
            this.SymbolsFile = string.Empty;
        }

        /// <summary>
        /// Gets or sets source code string
        /// </summary>
        public string SourceCode { get; set; }

        /// <summary>
        /// Gets or sets a path to the file from which source code is loaded
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Gets or sets the first line number (zero-based) of the source code fragment in the file 
        /// determined by <see cref="SourceFile"/> property
        /// </summary>
        public int LineStart { get; set; }

        /// <summary>
        /// Gets or sets the last line number (zero-based) of the source code fragment in the file 
        /// determined by <see cref="SourceFile"/> property
        /// </summary>
        public int LineEnd { get; set; }

        /// <summary>
        /// Gets or sets a path to the file from which the sequence points data for this source 
        /// code is loaded
        /// </summary>
        public string SymbolsFile { get; set; }

        /// <summary>
        /// Gets or sets a method which source code is contained by this instance
        /// </summary>
        public MethodBase Method { get; set; }

        /// <summary>
        /// Gets or sets the byte offset of the start of the bytecode fragment in the method body 
        /// </summary>
        public uint CilStart { get; set; }

        /// <summary>
        /// Gets or sets the byte offset of the end of the bytecode fragment in the method body 
        /// </summary>
        public uint CilEnd { get; set; }

        public SourceInfoError Error { get; set; }

        internal static readonly SourceInfo Empty = new SourceInfo();

        internal static SourceInfo FromError(SourceInfoError err)
        {
            return new SourceInfo() { Error = err };
        }
    }

    public enum SourceInfoError
    {
        Success = 0,
        InvalidFormat = 1,
        NoSourcePath = 2,
        NoMatches = 3,
        NoModulePath = 4
    }
}
