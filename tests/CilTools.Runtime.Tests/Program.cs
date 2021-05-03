/* CIL Tools tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using Microsoft.Diagnostics.Runtime;

namespace CilTools.Runtime.Tests
{
    class Program
    {
        static int Main(string[] args)
        {
            //start test app
            string path = @"..\..\..\EmitSampleApp\bin\Debug\net45\win-x86\EmitSampleApp.exe";
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = path;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process pr = Process.Start(psi);

            //wait for init
            Console.WriteLine("Running test...");
            Thread.Sleep(2000);

            //attach to process via ClrMD
            DataTarget dt = DataTarget.AttachToProcess(pr.Id, 5000, AttachFlag.NonInvasive);

            using (dt)
            {
                ClrInfo runtimeInfo = dt.ClrVersions[0];
                ClrRuntime runtime = runtimeInfo.CreateRuntime();
                ClrAssemblyReader reader = new ClrAssemblyReader(runtime);
                DynamicMethodsAssembly ass = reader.GetDynamicMethods();
                Type t = ass.ChildType;

                //get dynamic method
                MethodBase mb = (MethodBase)(t.GetMembers(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                    )[0]);

                string str = CilAnalysis.MethodToText(mb);

                AssertThat.IsMatch(str, new MatchElement[] {
                 new Literal(".method"), MatchElement.Any,
                 new Literal("Method1"), MatchElement.Any,
                 new Literal("cil"), MatchElement.Any,
                 new Literal("managed"), MatchElement.Any,
                 new Literal(".try"), MatchElement.Any,
                 new Literal("catch"), MatchElement.Any,
                 new Literal("System.Exception"), MatchElement.Any,
                 new Literal("call"), MatchElement.Any,
                 new Literal("System.Console::WriteLine"), MatchElement.Any,
                 new Literal("ret"), MatchElement.Any
                });
            }

            pr.Kill();
            Console.WriteLine("Test passed");
            return 0;
        }

        /*
        .method   public static class [???]??? Method1() cil managed
        {

         .try
         {
                   ldarg.0      
                   ldarg.1      
                   add          
                   stsfld       UnknownField4000002
                   leave        IL_0001
         }
         catch [mscorlib]System.Exception
         {
          IL_0001: leave        IL_0002
         }
         IL_0002: ldstr        UnknownString70000004
                  call         void [mscorlib]System.Console::WriteLine(string)
                  ldsfld       UnknownField4000006
                  ret          
        }
        */
    }
}
