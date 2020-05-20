﻿/* CIL Tools 
 * Copyright (c) 2020, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CilView
{
    class OpenFileOperation : OperationBase
    {
        string filename;
        FileAssemblySource result;

        public OpenFileOperation(string fname)
        {
            this.filename = fname;
        }

        public FileAssemblySource Result { get { return this.result; } }

        public override async Task Start()
        {
            this.ReportProgress("Loading assembly...", 0, 0);

            await Task.Run(() =>
            {
                FileAssemblySource res = new FileAssemblySource(this.filename);
                this.result = res;
            });
        }
    }
}
