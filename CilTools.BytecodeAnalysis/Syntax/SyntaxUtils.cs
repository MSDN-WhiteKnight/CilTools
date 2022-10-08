/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;

namespace CilTools.Syntax
{
    static class SyntaxUtils
    {
        internal static string GetIndentString(int c)
        {
            return "".PadLeft(c, ' ');
        }
    }
}
