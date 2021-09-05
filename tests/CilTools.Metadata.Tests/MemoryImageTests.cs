/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using CilTools.Reflection;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class MemoryImageTests
    {
        [TestMethod]
        public void Test_MemoryImage_Load()
        {
            //find corelib module
            string assname = typeof(object).Assembly.GetName().Name;
            string name = assname+".ni.dll";
            ProcessModule module = null;
            ProcessModuleCollection pmc = Process.GetCurrentProcess().Modules;
            
            foreach (ProcessModule m in pmc)
            {
                Logger.LogMessage(m.ModuleName);

                if (m.ModuleName.Equals(name, StringComparison.OrdinalIgnoreCase)) 
                {
                    module = m;
                    break;
                }
            }

            if (module == null) Assert.Fail(name+" module not found");

            //get corelib image as byte array
            byte[] imageBytes = new byte[module.ModuleMemorySize];
            Marshal.Copy(module.BaseAddress, imageBytes, 0, module.ModuleMemorySize);

            //load memory image
            MemoryImage img = new MemoryImage(imageBytes, module.FileName);

            AssemblyReader reader = new AssemblyReader();

            using (reader) 
            {
                Assembly ass = reader.LoadImage(img);
                Assert.IsNotNull(ass);
                Assert.AreEqual(assname, ass.GetName().Name);
                Type t = ass.GetType("System.Object");
                Assert.IsNotNull(t);
                Assert.AreEqual("System.Object", t.FullName);
            }
        }
    }
}
