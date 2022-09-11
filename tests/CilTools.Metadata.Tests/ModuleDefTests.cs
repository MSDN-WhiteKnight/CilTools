/* CilTools.Metadata tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Tests.Common;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class ModuleDefTests
    {
        [TestMethod]
        public void Test_ModuleDef()
        {
            AssemblyReader reader = ReaderFactory.GetReader();
            Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
            Module module = ass.ManifestModule;
            Module moduleReflection = typeof(SampleMethods).Assembly.ManifestModule;

            Assert.AreEqual("CilTools.Tests.Common.dll", module.Name);
            Assert.AreEqual("CilTools.Tests.Common.dll", module.ScopeName);
            Assert.AreEqual(typeof(SampleMethods).Assembly.Location, module.FullyQualifiedName);
            Assert.AreSame(ass, module.Assembly);
            Assert.AreEqual(1, module.MetadataToken);
            Assert.AreEqual(moduleReflection.ModuleVersionId, module.ModuleVersionId);
        }
    }
}
