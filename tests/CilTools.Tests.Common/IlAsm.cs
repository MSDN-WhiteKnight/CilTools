/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace CilTools.Tests.Common
{
    public class IlAsm
    {
        const string IlAsmCmd = " /NOLOGO /DLL /OUTPUT:\"{0}\" \"{1}\"";
        const string AssemblyDefIL = ".assembly CilProject_@Name { }\r\n";
        const string AssemblyFileName = "CilProject_{0}.dll";

        static string GetIlAsmDir()
        {
            //Hardcoded to .NET Framework path to work the same in any runtime.
            //.NET Core IlAsm is available as NuGet package, but it's simpler to 
            //use .NET Framework's
            string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            const string fxdir = "Microsoft.NET\\Framework\\v4.0.30319";
            return Path.Combine(windir, fxdir);
        }

        static string GetOutDir()
        {
            return Path.GetDirectoryName(typeof(IlAsm).Assembly.Location);
        }

        static string GetAssemblyDefIL(string name)
        {
            return AssemblyDefIL.Replace("@Name", name);
        }

        public static bool BuildFile(string inputFilePath, string outputFilePath)
        {
            if (File.Exists(outputFilePath))
            {
                Debug.WriteLine(outputFilePath + " already exists, deleting...");
                File.Delete(outputFilePath);
            }

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = Path.Combine(GetIlAsmDir(), "Ilasm.exe");
            psi.UseShellExecute = false;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;
            psi.Arguments = string.Format(IlAsmCmd, outputFilePath, inputFilePath);
            Debug.WriteLine(">" + psi.FileName + " " + psi.Arguments);

            Process pr = new Process();

            using (pr)
            {
                pr.StartInfo = psi;
                pr.Start();
                string s = pr.StandardOutput.ReadToEnd(); //получение вывода
                pr.WaitForExit();
                Debug.WriteLine(s);

                if (pr.ExitCode != 0)
                {
                    Debug.WriteLine("IlAsm failed");
                    return false;
                }

                //build succeeded
                return true;
            }
        }

        public static bool BuildCode(string inputCode, string outputFilePath)
        {
            string path = Path.Combine(GetOutDir(), "temp.il");
            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            StreamWriter wr = new StreamWriter(fs, Encoding.UTF8);

            using (wr)
            {
                wr.Write(inputCode);
            }

            return BuildFile(path, outputFilePath);
        }

        public static MethodBase BuildFunction(string funcCode, string funcName)
        {
            StringBuilder sb = new StringBuilder(funcCode.Length + AssemblyDefIL.Length + 10);
            sb.AppendLine(GetAssemblyDefIL(funcName));
            sb.AppendLine(funcCode);
            string inputCode = sb.ToString();

            string filename = string.Format(AssemblyFileName, funcName);
            string assemblyPath = Path.Combine(GetOutDir(), filename);
            bool res = BuildCode(inputCode, assemblyPath);

            if (res == false) return null;

            Assembly ass = Assembly.LoadFrom(assemblyPath);
            MethodBase mb = ass.ManifestModule.GetMethod(funcName);
            return mb;
        }
    }
}
