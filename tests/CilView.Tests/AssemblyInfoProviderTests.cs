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
                CilViewTestsCore.VerifyAssemblyInfoText(info, typeof(SampleMethods).Assembly.Location);
            }
        }
    }
}
