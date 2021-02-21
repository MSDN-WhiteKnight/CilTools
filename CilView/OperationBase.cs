/* CIL Tools 
 * Copyright (c) 2020, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CilView.UI.Dialogs;

namespace CilView
{
    public abstract class OperationBase
    {
        volatile bool shouldStop=false;

        public ProgressWindow Window { get; set; }

        public abstract Task Start();

        public void Stop()
        {
            this.shouldStop = true;
        }

        public bool Stopped { get { return this.shouldStop; } }

        public virtual void DoEvents()
        {
            
        }

        public void ReportProgress(string txt, double curr, double max)
        {
            if (this.Window != null) this.Window.ReportProgress(txt,curr,max);
        }
    }
}
