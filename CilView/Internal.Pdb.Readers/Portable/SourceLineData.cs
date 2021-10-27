/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;

namespace Internal.Pdb.Portable
{
    class SourceLineData
    {
        public string FilePath { get; set; }
        public int CilOffset { get; set; }
        public int LineStart { get; set; }
        public int LineEnd { get; set; }
        public int ColStart { get; set; }
        public int ColEnd { get; set; }
    }
}
