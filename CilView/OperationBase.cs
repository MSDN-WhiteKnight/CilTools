/* CIL Tools 
 * Copyright (c) 2020, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CilView
{
    public abstract class OperationBase
    {
        volatile bool shouldStop=false;

        public abstract Task Start();

        public void Stop()
        {
            this.shouldStop = true;
        }

        protected bool ShouldStop { get { return this.shouldStop; } }
    }
}
