/* CIL Tools tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Diagnostics;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.CommandLine.Tests
{
    [TestClass]
    public class CommandLineTests
    {
        class CommandResult 
        {
            public int ExitCode { get; set; }
            public string StandardOutputText { get; set; }
            public string StandardErrorText { get; set; }

            public bool Success { get { return this.ExitCode == 0; } }
        }

        static CommandResult Execute(string command, string args)
        {
            string path = Path.GetDirectoryName(typeof(CommandLineTests).Assembly.Location);
            Directory.SetCurrentDirectory(path);

            CommandResult ret = new CommandResult();
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = command;
            psi.UseShellExecute = false;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.Arguments = args;
            Process pr = new Process();

            using (pr)
            {
                pr.StartInfo = psi;
                pr.Start();
                ret.StandardOutputText = pr.StandardOutput.ReadToEnd(); //получение вывода
                bool exited = pr.WaitForExit(10000);

                if (!exited) throw new TimeoutException("Process.WaitForExit timed out");

                ret.StandardErrorText = pr.StandardError.ReadToEnd();
                ret.ExitCode = pr.ExitCode;
                return ret;
            }
        }

        const string exepath = "../../../../../CilTools.CommandLine/bin/{0}/netcoreapp3.1/cil";

        static string GetExePath()
        {
            return string.Format(exepath, Utils.GetConfig());
        }

        [TestMethod]
        public void Test_View_PrintHelloWorld()
        {
            const string cmd = "view CilTools.Tests.Common.dll CilTools.Tests.Common.SampleMethods PrintHelloWorld";
            CommandResult cr = Execute(GetExePath(), cmd);
            string s = cr.StandardOutputText;
            Console.WriteLine(s);
            Console.WriteLine(cr.StandardErrorText);
            Assert.IsTrue(cr.Success);

            AssertThat.IsMatch(s, new Text[] {
                "*** CIL Tools command line ***", Text.Any, "Assembly: CilTools.Tests.Common.dll" , Text.Any,
                "CilTools.Tests.Common.SampleMethods.PrintHelloWorld", Text.Any
            });

            CilGraphTestsCore.Assert_CilGraph_HelloWorld(s);
        }

        [TestMethod]
        public void Test_Disasm()
        {
            const string cmd = "disasm --output test.il CilTools.Tests.Common.dll CilTools.Tests.Common.SampleMethods PrintTenNumbers";
            CommandResult cr = Execute(GetExePath(), cmd);
            string s = cr.StandardOutputText;
            Console.WriteLine(s);
            Console.WriteLine(cr.StandardErrorText);
            Assert.IsTrue(cr.Success);

            AssertThat.IsMatch(s, new Text[] {
                "*** CIL Tools command line ***", Text.Any, "Input file: CilTools.Tests.Common.dll" , Text.Any,
                "Disassembling CIL...", Text.Any, "Output successfully written to test.il", Text.Any
            });

            string output = File.ReadAllText("test.il");
            Console.WriteLine(output);
            CilGraphTestsCore.Assert_CilGraph_Loop(output);
        }
    }
}
