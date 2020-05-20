/* CIL Tools 
 * Copyright (c) 2020, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CilView
{
    class OpenProcessOperation:OperationBase
    {
        Process process;
        bool activemode;
        ProcessAssemblySource result;

        public OpenProcessOperation(Process pr, bool active)
        {
            this.process = pr;
            this.activemode = active;
        }

        public ProcessAssemblySource Result { get { return this.result; } }

        internal static void DoWpfEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                 new DispatcherOperationCallback((f) =>
                 {
                     ((DispatcherFrame)f).Continue = false; return null;
                 }), frame);
            Dispatcher.PushFrame(frame);
        }

        public override void DoEvents()
        {
            DoWpfEvents();
        }

        public override Task Start()
        {
            this.ReportProgress("Attaching to process...", 0, 0);
            this.DoEvents();

            ProcessAssemblySource res = new ProcessAssemblySource(this.process, this.activemode, this);

            if (this.Stopped) res.Dispose();
            else this.result = res;

            return Task.FromResult<bool>(true);
        }
    }
}
