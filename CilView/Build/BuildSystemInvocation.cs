﻿/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
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

        static List<string> s_tempdirs = new List<string>(20);
        static readonly object s_tempdirsSync = new object();

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

        static bool CleanupTempFiles_ProcessDirectory(string dir)
        {
            string[] files = Directory.GetFiles(dir);
            bool success = true;

            //delete files in a dir
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    File.Delete(files[i]);
                }
                catch (Exception ex)
                {
                    success = false;
                    ErrorHandler.Current.Error(ex, "Removing temp file "+files[i],true);
                }
            }

            //delete subdirs
            string[] subdirs = Directory.GetDirectories(dir);

            for (int i = 0; i < subdirs.Length; i++)
            {
                bool res=CleanupTempFiles_ProcessDirectory(subdirs[i]);
                if (res == false) success = false;
            }

            //if all files and subdirs are removed, we can remove the whole directory
            if (success)
            {
                try
                {
                    Directory.Delete(dir);
                }
                catch (Exception ex)
                {
                    ErrorHandler.Current.Error(ex, "Removing temp dir " + dir, true);
                }
            }

            return success;
        }

        public static void CleanupTempFiles()
        {
            lock (s_tempdirsSync)
            {
                //remove registered temp directories
                for (int i = 0; i < s_tempdirs.Count; i++)
                {
                    CleanupTempFiles_ProcessDirectory(s_tempdirs[i]);
                }

                //cleanup the list of temp directories
                s_tempdirs.Clear();
            }
        }

        public static void RegisterTempDir(string dir)
        {
            lock (s_tempdirsSync)
            {
                s_tempdirs.Add(dir);
            }
        }

        static string PreparePath(string input)
        {
            if (String.IsNullOrEmpty(input)) return String.Empty;

            //ensure trailing backslash            
            if (input[input.Length - 1] != '\\')
            {
                input += "\\";
            }

            //escape backslash
            //this is needed to set IntermediateOutputPath properly 
            //in MSBuild command line
            input += "\\";
            return input;
        }

        public static string CreateTempDir(string proj,string dirname)
        {
            string t = Path.GetTempPath();
            string ret = String.Empty;
            Random rnd = new Random();
            int n = 0;

            //generate unique temp directory path

            while (true)
            {
                int x = rnd.Next();
                string name = proj+"_"+dirname+x.ToString("X");
                ret = Path.Combine(t, name);

                if (!File.Exists(ret) && !Directory.Exists(ret))
                {
                    //found unique name
                    Directory.CreateDirectory(ret);
                    return ret;
                }

                n++;

                if (n > 2000) throw new IOException("Failed to generate temp directory name");
            }
        }

        static string FindOutputFile(string dir, string name, string ext)
        {
            try
            {
                //try the same name as project
                string ret = Path.Combine(dir, name + ext);

                if (File.Exists(ret)) return ret;

                //if not found, fallback to any file with desired extension
                string[] files = Directory.GetFiles(dir, "*." + ext);

                if (files.Length > 0)
                {
                    return files[0];
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex, "BuildSystemInvocation.FindOutputFile", true);
            }

            return String.Empty;
        }

        bool InvokeProcess(ProcessStartInfo psi, string outputPath,string ext)
        {
            Process pr = new Process();

            using (pr)
            {
                pr.StartInfo = psi;
                pr.Start();
                string s = pr.StandardOutput.ReadToEnd(); //получение вывода
                pr.WaitForExit();
                this._output += s;
                this._output += Environment.NewLine;

                if (pr.ExitCode == 0)
                {
                    //build succeeded
                    string binpath = FindOutputFile(outputPath, this._InputProject.Name, ext);

                    if (String.IsNullOrEmpty(binpath)) 
                    {
                        this._output += "Error: Can't find project output file.";
                        this._output += Environment.NewLine;
                        this._success = false;
                    }
                    else 
                    {
                        //found output file
                        this._success = true;
                        this._binpath = binpath;
                    }
                }
                else
                {
                    //build failed
                    this._success = false;
                }

                this._invoked = true;
            }//end using

            return this._success;
        }
        
        bool InvokeMsbuild()
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.FileName = Path.Combine(
                RuntimeEnvironment.GetRuntimeDirectory(),
                "msbuild.exe"
                );

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
            string outputPath = CreateTempDir(this._InputProject.Name,"build");
            string objPath = CreateTempDir(this._InputProject.Name, "obj");

            //register in global temp dirs list
            RegisterTempDir(outputPath);
            RegisterTempDir(objPath);

            objPath = PreparePath(objPath);
            psi.Arguments = "-p:OutputPath=\"" + outputPath + "\"";
            psi.Arguments += " -p:IntermediateOutputPath=\"" + objPath + "\"";
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
            
            return this.InvokeProcess(psi,outputPath,ext);
        }//end InvokeMsbuild()

        bool InvokeDotnetBuild()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "C:\\Program Files\\dotnet\\dotnet.exe";

            if (!File.Exists(psi.FileName))
            {
                this._output += ".NET CLI not found";
                this._output += Environment.NewLine;
                return false;
            }

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

            string outputPath=CreateTempDir(this._InputProject.Name,"build");
            string objPath=CreateTempDir(this._InputProject.Name, "obj");

            //register in global temp dirs list
            RegisterTempDir(outputPath);
            RegisterTempDir(objPath);

            psi.Arguments = "build -o \"" + outputPath + "\"";

            if (!String.IsNullOrEmpty(tfm))
            {
                psi.Arguments += " -f " + tfm;
            }

            objPath = PreparePath(objPath);
            psi.Arguments += " -p:IntermediateOutputPath=\"" + objPath + "\"";
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
            
            return this.InvokeProcess(psi, outputPath, ext);
        }//end InvokeDotnetBuild()

        public bool Invoke()
        {
            bool res = this.InvokeDotnetBuild();

            if (res == false) res = this.InvokeMsbuild();

            return res;
        }

    }
}
