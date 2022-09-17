/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Tests.Common
{
    public static class CilViewTestsCore
    {
        public static void VerifyAssemblyInfoText(string info, string location)
        {
            Assert.IsTrue(info.Contains("Name: CilTools.Tests.Common"));
            Assert.IsTrue(info.Contains("Version: " + typeof(SampleMethods).Assembly.GetName().Version.ToString()));
            Assert.IsTrue(info.Contains("Location: " + location));
            Assert.IsTrue(info.Contains("Type: DLL"));
            Assert.IsTrue(info.Contains("Subsystem: WindowsCui"));
            Assert.IsTrue(info.Contains("CorFlags: 0x1 (ILOnly; )"));

            Assert.IsTrue(info.Contains("Referenced unmanaged modules"));
            Assert.IsTrue(info.ToLower().Contains("user32.dll"));

            AssemblyName anCilTools = typeof(CilViewTestsCore).Assembly.GetName();
            Assert.IsTrue(info.Contains("Referenced assemblies"));
            Assert.IsTrue(info.Contains("CilTools.BytecodeAnalysis, Version: " + anCilTools.Version.ToString()));
            Assert.IsTrue(info.Contains("CilTools.Metadata, Version: " + anCilTools.Version.ToString()));

            Assert.IsTrue(info.Contains("Assembly custom attributes"));
            Assert.IsTrue(info.Contains("TargetFrameworkAttribute"));

            Assert.IsTrue(info.Contains(
                "AssemblyCompanyAttribute\n01 00 09 43 49 4C 20 54 6F 6F 6C 73 00 00 \n(...CIL Tools..)\n"
                ));

            Assert.IsTrue(info.Contains(
                "AssemblyProductAttribute\n01 00 09 43 49 4C 20 54 6F 6F 6C 73 00 00 \n(...CIL Tools..)\n"
                ));

            Assert.IsTrue(info.Contains("AssemblyTitleAttribute\n" +
                "01 00 15 43 69 6C 54 6F 6F 6C 73 2E 54 65 73 74 73 2E 43 6F 6D 6D 6F 6E 00 00 \n" +
                "(...CilTools.Tests.Common..)\n"));
        }
    }
}
