/* CilTools.Metadata tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Reflection;
using CilTools.Tests.Common;
using CilTools.Tests.Common.Attributes;
using CilTools.Tests.Common.TextUtils;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class MetadataAssemblyTests
    {
        [TestMethod]
        public void Test_MetadataAssembly_InfoText()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            string info = (string)ReflectionInfoProperties.GetProperty(ass, ReflectionInfoProperties.InfoText);

            Assert.IsFalse(string.IsNullOrEmpty(info));
            Assert.IsTrue(info.Contains("Type: DLL"));
            Assert.IsTrue(info.Contains("Subsystem: WindowsCui"));
            Assert.IsTrue(info.Contains("File alignment: 512"));
            Assert.IsTrue(info.Contains("Section alignment: 8192"));
            Assert.IsTrue(info.Contains("OS version: 4.0"));
            Assert.IsTrue(info.Contains("Machine type: I386"));
            Assert.IsTrue(info.Contains("Characteristics: 0x2022 (ExecutableImage; LargeAddressAware; Dll; )"));
            Assert.IsTrue(info.Contains("CorFlags: 0x1 (ILOnly; )"));
        }

        [ConditionalTest(TestCondition.NetFrameworkOnly, "Depends on strong names")]
        public void Test_MetadataAssembly_InfoText_StrongName()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(object).Assembly.Location);
            string info = (string)ReflectionInfoProperties.GetProperty(ass, ReflectionInfoProperties.InfoText);

            Assert.IsFalse(string.IsNullOrEmpty(info));
            Assert.IsTrue(info.Contains("Type: DLL"));
            Assert.IsTrue(info.Contains("Subsystem: WindowsCui"));
            AssertThat.IsMatch(info, new Text[] {
                Text.Any, "CorFlags: ", Text.Any, "StrongNameSigned;", Text.Any 
            });
        }
    }
}
