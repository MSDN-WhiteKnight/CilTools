/* CilTools.Metadata tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
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
            string info = (string)ReflectionProperties.Get(ass, ReflectionProperties.InfoText);

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
            string info = (string)ReflectionProperties.Get(ass, ReflectionProperties.InfoText);

            Assert.IsFalse(string.IsNullOrEmpty(info));
            Assert.IsTrue(info.Contains("Type: DLL"));
            Assert.IsTrue(info.Contains("Subsystem: WindowsCui"));
            AssertThat.IsMatch(info, new Text[] {
                Text.Any, "CorFlags: ", Text.Any, "StrongNameSigned;", Text.Any 
            });
        }

        [TestMethod]
        public void Test_GetReferencedAssemblies()
        {
            AssemblyName anBytecodeAnalysis = typeof(ICustomMethod).Assembly.GetName();
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            AssemblyName[] refs = ass.GetReferencedAssemblies();

            AssertThat.HasOnlyOneMatch(refs, 
                x => x.Name == "CilTools.BytecodeAnalysis" && x.Version == anBytecodeAnalysis.Version);

            AssertThat.HasOnlyOneMatch(refs,
                x => x.Name == "CilTools.Metadata" && x.Version == anBytecodeAnalysis.Version);
        }

        [ConditionalTest(TestCondition.NetFrameworkOnly, "Depends on corlib name")]
        public void Test_GetReferencedAssemblies_CorLib()
        {
            AssemblyName anCorelib = typeof(object).Assembly.GetName();
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            AssemblyName[] refs = ass.GetReferencedAssemblies();

            AssertThat.HasOnlyOneMatch(refs, x => x.Name == "mscorlib" && x.Version == anCorelib.Version &&
                x.GetPublicKeyToken().SequenceEqual(anCorelib.GetPublicKeyToken()));
        }

        [TestMethod]
        public void Test_GetReferencedModules()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            string[] modules = (string[])ReflectionProperties.Get(ass, ReflectionProperties.ReferencedModules);

            Assert.AreEqual(1, modules.Length);
            Assert.AreEqual("user32.dll", modules[0], ignoreCase: true);
        }

        [TestMethod]
        public void Test_GetReferencedModules_Negative()
        {
            AssemblyReader reader = ReaderFactory.GetReader();

            //load CilTools.BytecodeAnalysis
            Assembly ass = reader.LoadFrom(typeof(ReflectionProperties).Assembly.Location);
            string[] modules = (string[])ReflectionProperties.Get(ass, ReflectionProperties.ReferencedModules);

            Assert.AreEqual(0, modules.Length);
        }

        [TestMethod]
        public void Test_Subsystem()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            int subsystem = (int)ReflectionProperties.Get(ass, ReflectionProperties.Subsystem);

            Assert.AreEqual(0x0003, subsystem); //WINDOWS_CUI
        }

        [TestMethod]
        public void Test_CorFlags()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            int subsystem = (int)ReflectionProperties.Get(ass, ReflectionProperties.CorFlags);

            Assert.AreEqual(0x0001, subsystem); //ILONLY
        }
    }
}
