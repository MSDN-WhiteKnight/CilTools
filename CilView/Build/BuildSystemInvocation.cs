/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using CilView.Common;

namespace CilView.Build
{
    class BuildSystemInvocation
    {
        ProjectInfo _InputProject;
        bool _invoked;
        bool _success;
        string _output;
        string _binpath;

        public BuildSystemInvocation(ProjectInfo inputProject)
        {
            this._InputProject = inputProject;
            this._invoked = false;
            this._output = String.Empty;
            this._binpath = String.Empty;
        }

        public bool IsInvoked
        {
            get { return this._invoked; }
        }

        public bool IsSuccessful
        {
            get { return this._success; }
        }

        public string OutputText
        {
            get { return this._output; }
        }

        public string BinaryPath
        {
            get { return this._binpath; }
        }
        
        public bool Invoke()
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = "C:\\Program Files\\dotnet\\dotnet.exe";

            string ext = ".dll";

            if (!this._InputProject.IsSDK)
            {
                if (Utils.StringEqualsIgnoreCase(this._InputProject.OutputType, "Exe") ||
                   Utils.StringEqualsIgnoreCase(this._InputProject.OutputType, "WinExe"))
                {
                    ext = ".exe";
                }
            }

            string projectDir = Path.GetDirectoryName(this._InputProject.ProjectPath);
            string tfm = String.Empty;

            if (this._InputProject.TargetFrameworks.Length > 0)
            {
                tfm = this._InputProject.TargetFrameworks[0];
            }

            string outputPath;
            if (!String.IsNullOrEmpty(tfm))
            {
                outputPath = Path.Combine(projectDir, "bin\\Debug", tfm);
            }
            else
            {
                outputPath = Path.Combine(projectDir, "bin\\Debug");
            }
                
            psi.Arguments = "build -o \""+ outputPath+ "\"";

            if (!String.IsNullOrEmpty(tfm))
            {
                psi.Arguments += " -f " + tfm;
            }

            psi.Arguments += " \"" + this._InputProject.ProjectPath + "\"";

            if (CultureInfo.InstalledUICulture != null)
            {
                string culture = CultureInfo.InstalledUICulture.Name;

                if (Utils.StringEqualsIgnoreCase(culture, "ru-ru"))
                {
                    psi.StandardOutputEncoding = Encoding.GetEncoding(866);
                }
            }
            
            psi.UseShellExecute = false;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;

            Process pr = new Process();

            using (pr)
            {
                pr.StartInfo = psi;
                pr.Start();
                string s = pr.StandardOutput.ReadToEnd(); //получение вывода
                pr.WaitForExit();
                this._output = s;

                if (pr.ExitCode == 0)
                {
                    this._success = true;
                    this._binpath = Path.Combine(outputPath, this._InputProject.Name + ext);
                }
                else
                {
                    this._success = false;
                }
                
                this._invoked = true;
            }//end using

            return this._success;
        }//end Invoke()

    }
}
