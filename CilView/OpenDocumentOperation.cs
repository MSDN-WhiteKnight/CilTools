/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CilView
{
    /// <summary>
    /// Represents a backgound operation of loading and parsing .il source document
    /// </summary>
    class OpenDocumentOperation : OperationBase
    {
        string filename;
        IlasmAssemblySource result;
        Exception error = null;

        public OpenDocumentOperation(string fname)
        {
            this.filename = fname;
        }

        public IlasmAssemblySource Result { get { return this.result; } }

        public override async Task Start()
        {
            this.ReportProgress("Loading document...", 0, 0);

            await Task.Run(() =>
            {
                try
                {
                    IlasmAssemblySource res = new IlasmAssemblySource(this.filename);
                    this.result = res;
                }
                catch (Exception ex)
                {
                    this.error = ex;
                    this.result = null;
                }
            });

            if (this.error != null)
            {
                ErrorHandler.Current.Error(this.error);
            }
        }
    }
}
