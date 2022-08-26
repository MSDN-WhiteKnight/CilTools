/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Metadata;
using CilTools.Tests.Common;
using CilView.Core.Reflection;

namespace CilView.Tests
{
    [TestClass]
    public class AssemblyInfoProviderTests
    {
        [TestMethod]
        public void Test_GetAssemblyInfo_RuntimeReflection()
        {
            string info = AssemblyInfoProvider.GetAssemblyInfo(typeof(SampleMethods).Assembly);

            Assert.IsFalse(string.IsNullOrEmpty(info));
            Assert.IsTrue(info.Contains("Name: CilTools.Tests.Common"));
            Assert.IsTrue(info.Contains("Version: "+ typeof(SampleMethods).Assembly.GetName().Version.ToString()));
            Assert.IsTrue(info.Contains("Location: " + typeof(SampleMethods).Assembly.Location));

            AssemblyName anCilTools = typeof(AssemblyInfoProvider).Assembly.GetName();
            Assert.IsTrue(info.Contains("Referenced assemblies"));
            Assert.IsTrue(info.Contains("CilTools.BytecodeAnalysis, Version: " + anCilTools.Version.ToString()));
            Assert.IsTrue(info.Contains("CilTools.Metadata, Version: " + anCilTools.Version.ToString()));
        }

        [TestMethod]
        public void Test_GetAssemblyInfo()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                string info = AssemblyInfoProvider.GetAssemblyInfo(ass).Replace("\r\n", "\n");

                Assert.IsFalse(string.IsNullOrEmpty(info));
                Assert.IsTrue(info.Contains("Name: CilTools.Tests.Common"));
                Assert.IsTrue(info.Contains("Version: " + typeof(SampleMethods).Assembly.GetName().Version.ToString()));
                Assert.IsTrue(info.Contains("Location: " + typeof(SampleMethods).Assembly.Location));
                Assert.IsTrue(info.Contains("Type: DLL"));
                Assert.IsTrue(info.Contains("Subsystem: WindowsCui"));
                Assert.IsTrue(info.Contains("CorFlags: 0x1 (ILOnly; )"));

                AssemblyName anCilTools = typeof(AssemblyInfoProvider).Assembly.GetName();
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

                Assert.IsTrue(info.Contains("AssemblyTitleAttribute\n"+
                    "01 00 15 43 69 6C 54 6F 6F 6C 73 2E 54 65 73 74 73 2E 43 6F 6D 6D 6F 6E 00 00 \n"+
                    "(...CilTools.Tests.Common..)\n"));
            }
        }
    }
}
