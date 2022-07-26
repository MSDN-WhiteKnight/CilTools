/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.CommandLine
{
    abstract class Command
    {
        public abstract string Name {get;}
        
        public abstract string Description {get;}
        
        public abstract string UsageDocumentation {get;}
        
        public abstract int Execute(string[] args);
    }
}
