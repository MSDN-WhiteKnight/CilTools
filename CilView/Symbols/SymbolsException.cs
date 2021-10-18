/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.Symbols
{
    class SymbolsException:Exception
    {
        public SymbolsException(string message) : base(message) { }
    }
}
