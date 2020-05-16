/* CIL Tools 
 * Copyright (c) 2020, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CilView
{
    class LoadProcessOperation:OperationBase
    {
        Process process;
        bool activemode;
        ProcessAssemblySource result;

        public LoadProcessOperation(Process pr, bool active)
        {
            this.process = pr;
            this.activemode = active;
        }

        //public ProcessAssemblySource Result { get { return this.result; } }

        public override Task Start()
        {
            return Task.FromResult<bool>(true);
        }
    }
}
