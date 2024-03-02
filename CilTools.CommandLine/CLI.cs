/* CIL Tools
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CilTools.CommandLine
{
    static class CLI
    {
        public const string Copyright = "Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight/CilTools)";
        public const string HtmlFileName = "CilTools.html";

        public static string GetErrorInfo()
        {
            string exeName = typeof(Program).Assembly.GetName().Name;
            return "Use \"" + exeName + " help\" to print the list of available commands and their arguments.";
        }

        public static FileStream TryCreateFile(string name)
        {
            FileStream fs;

            try
            {
                //try in current directory
                fs = new FileStream(name, FileMode.Create, FileAccess.Write);
            }
            catch (IOException)
            {
                //try in temp directory
                string path = Path.Combine(Path.GetTempPath(), name);
                fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            }

            return fs;
        }

        public static void OpenInBrowser(string filePath)
        {
            Console.WriteLine("Trying to open " + filePath + " in browser...");
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = filePath;
            psi.UseShellExecute = true;
            Process pr = Process.Start(psi);

            if (pr != null) pr.Dispose();
        }
    }
}
