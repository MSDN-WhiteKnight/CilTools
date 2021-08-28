/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CilView.Build
{
    class BuildProjectOperation : OperationBase
    {
        //represents a cancellable operation that builds the specified MSBuild project
        string filename;
        BuildSystemInvocation result;
        Exception error = null;

        public BuildProjectOperation(string fname)
        {
            this.filename = fname;
        }

        public BuildSystemInvocation Result { get { return this.result; } }

        public override async Task Start()
        {
            this.ReportProgress("Building project...", 0, 0);

            await Task.Run(() =>
            {
                try
                {
                    ProjectInfo info = ProjectInfo.ReadFile(this.filename);
                    BuildSystemInvocation builder = new BuildSystemInvocation(info);
                                                                                
                    //build the project
                    builder.Invoke();

                    //If the operation is cancelled here, we don't stop the build process, just let 
                    //it finish and discard the results. This is probably OK because we use temp dirs
                    //with random unique names for build results and so the discarded process
                    //has very little chance to clash with other builds.

                    this.result = builder;
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
