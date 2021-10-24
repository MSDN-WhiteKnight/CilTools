/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;

namespace CilView.Symbols
{
    /// <summary>
    /// Specifies how to determine the bytecode fragment when reading symbols
    /// </summary>
    enum SymbolsQueryType
    {
        /// <summary>
        /// Query the bytecode range between the specified start and end offsets 
        /// </summary>
        RangeExact=1,

        /// <summary>
        /// Query the sequence point in which the start offset lies. Ignore the end offset.
        /// </summary>
        SequencePoint=2
    }
}
