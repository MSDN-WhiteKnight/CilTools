/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Runtime;

namespace CilView
{
    class OpenProcessOperation:OperationBase
    {
        Process process;
        bool activemode;
        AssemblySource result;

        public OpenProcessOperation(Process pr, bool active)
        {
            this.process = pr;
            this.activemode = active;
        }

        public AssemblySource Result { get { return this.result; } }

        public override void DoEvents()
        {
            Common.Utils.DoWpfEvents();
        }

        public override Task Start()
        {
            this.ReportProgress("Attaching to process...", 0, 0);
            this.DoEvents();

            AssemblySource res=null;
            try
            {
                res = new ProcessAssemblySource(this.process, this.activemode, this);
            }
            catch (Exception ex) when (ex is ClrDiagnosticsException || ex is ProcessLoadException)
            {
                res = new WmiAssemblySource(this.process, this);
            }

            if (this.Stopped) res.Dispose();
            else this.result = res;

            return Task.FromResult<bool>(true);
        }
    }
}
