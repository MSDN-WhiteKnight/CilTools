/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.Core
{
    public static class FileUtils
    {
        public static bool HasCilSourceExtension(string file)
        {
            return file.EndsWith(".il", StringComparison.OrdinalIgnoreCase) ||                
                   file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasPeFileExtension(string file)
        {
            return file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                   file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        }
    }
}
