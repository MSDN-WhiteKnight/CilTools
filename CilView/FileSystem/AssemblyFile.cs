/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.FileSystem
{
    public class AssemblyFile
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public override string ToString()
        {
            if (this.Name == null) return "(AssemblyFile)";
            else return this.Name;
        }
    }
}
