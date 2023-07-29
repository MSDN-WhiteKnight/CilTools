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

        [TestMethod]
        public void Test_ForwardedTypes_Empty()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            Type[] forwards = (Type[])ReflectionProperties.Get(ass, ReflectionProperties.ForwardedTypes);

            Assert.AreEqual(0, forwards.Length);
        }

        [TestMethod]
        public void Test_ResolveMethod()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            ITokenResolver resolver = (ITokenResolver)ass;

            MethodInfo mi = typeof(SampleMethods).GetMethod("PrintHelloWorld");

            //valid
            MethodBase miResolved = resolver.ResolveMethod(mi.MetadataToken);
            Assert.AreEqual(mi.Name, miResolved.Name);
            Assert.AreEqual(mi.MetadataToken, miResolved.MetadataToken);

            //invalid
            Assert.IsNull(resolver.ResolveMethod(0));
            Assert.IsNull(resolver.ResolveMethod(0x4000001)); //Field
            AssertThat.Throws<ArgumentOutOfRangeException>(() => resolver.ResolveMethod(0x6FFFFFF)); //MethodDef
            AssertThat.Throws<ArgumentOutOfRangeException>(() => resolver.ResolveMethod(0xAFFFFFF)); //MemberRef
        }

        [TestMethod]
        public void Test_ResolveField()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            ITokenResolver resolver = (ITokenResolver)ass;

            FieldInfo fi = typeof(SampleMethods).GetField("Foo");

            //valid
            FieldInfo fiResolved = resolver.ResolveField(fi.MetadataToken);
            Assert.AreEqual(fi.Name, fiResolved.Name);
            Assert.AreEqual(fi.MetadataToken, fiResolved.MetadataToken);

            //invalid
            Assert.IsNull(resolver.ResolveField(0));
            Assert.IsNull(resolver.ResolveField(0x6000001)); //MethodDef
            AssertThat.Throws<ArgumentOutOfRangeException>(() => resolver.ResolveField(0x4FFFFFF)); //Field
            AssertThat.Throws<ArgumentOutOfRangeException>(() => resolver.ResolveField(0xAFFFFFF)); //MemberRef
        }

        [TestMethod]
        public void Test_ResolveType()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            ITokenResolver resolver = (ITokenResolver)ass;
            
            //valid
            Type tResolved = resolver.ResolveType(typeof(SampleMethods).MetadataToken);
            Assert.AreEqual(typeof(SampleMethods).FullName, tResolved.FullName);
            Assert.AreEqual(typeof(SampleMethods).MetadataToken, tResolved.MetadataToken);

            //invalid
            Assert.IsNull(resolver.ResolveType(0));
            Assert.IsNull(resolver.ResolveType(0x6000001)); //MethodDef
            AssertThat.Throws<ArgumentOutOfRangeException>(() => resolver.ResolveType(0x2FFFFFF)); //TypeDef
            AssertThat.Throws<ArgumentOutOfRangeException>(() => resolver.ResolveType(0x1FFFFFF)); //TypeRef
            AssertThat.Throws<ArgumentOutOfRangeException>(() => resolver.ResolveType(0x1BFFFFFF)); //TypeSpec
        }
    }
}
