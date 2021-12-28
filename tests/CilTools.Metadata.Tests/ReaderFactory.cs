/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Metadata.Tests
{
    static class ReaderFactory
    {
        static readonly AssemblyReader s_globalReader = new AssemblyReader();

        public static AssemblyReader GetReader()
        {
            return s_globalReader;
        }
    }
}
