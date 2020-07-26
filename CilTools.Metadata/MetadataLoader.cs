/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Metadata
{
    public static class MetadataLoader
    {
        public static IEnumerable<MethodBase> EnumerateMethods(string path)
        {
            MetadataAssembly ass = new MetadataAssembly(path);

            return ass.EnumerateMethods();
        }

        public static MetadataAssembly Load(string path)
        {
            return new MetadataAssembly(path);
        }
    }
}
