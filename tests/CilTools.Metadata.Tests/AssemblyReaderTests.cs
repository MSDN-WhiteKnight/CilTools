/* CilTools.Metadata tests
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilTools.Tests.Common;
using CilTools.Tests.Common.TextUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class AssemblyReaderTests
    {
        [TestMethod]
        public void Test_AssemblyReader_Method()
        {
            AssemblyReader reader = new AssemblyReader();
            MethodInfo m = null;

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                m = t.GetMember("PrintHelloWorld")[0] as MethodInfo;

                Assert.AreEqual("PrintHelloWorld", m.Name);
                Assert.AreEqual(MemberTypes.Method, m.MemberType);
                Assert.AreEqual("Void",m.ReturnType.Name);
                Assert.AreEqual(0, m.GetParameters().Length);
            }
        }

        [TestMethod]
        public void Test_Constructor()
        {
            AssemblyReader reader = new AssemblyReader();
            MethodBase m = null;

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(TestType).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.TestType");
                m = t.GetMember(".ctor", Utils.AllMembers())[0] as MethodBase;

                Assert.AreEqual(".ctor", m.Name);
                Assert.AreEqual(MemberTypes.Constructor, m.MemberType);
                Assert.IsNull((m as ICustomMethod).ReturnType);
            }
        }

        [TestMethod]
        public void Test_AssemblyReader_Method_SameInstance()
        {
            AssemblyReader reader = new AssemblyReader();            

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase m = t.GetMember("PrintHelloWorld")[0] as MethodBase;
                MethodBase m2 = t.GetMember("PrintHelloWorld")[0] as MethodBase;
                Assert.AreSame(m, m2);
            }
        }

        [TestMethod]
        public void Test_EntryPoint()
        {
            if(typeof(object).Assembly.GetName().Name == "System.Private.CoreLib")
            {
                throw new AssertInconclusiveException("Skipped on .NET Core due to bugs");
            }
            
            string dir = Path.GetDirectoryName(typeof(AssemblyReaderTests).Assembly.Location);
            Directory.SetCurrentDirectory(dir);

            string path = string.Format(
                @"..\..\..\EmitSampleApp\bin\{0}\net45\win-x86\EmitSampleApp.exe", 
                Utils.GetConfig());

            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(path);
                MethodInfo entryPoint = ass.EntryPoint;
                Assert.AreEqual("Main", entryPoint.Name);

                string str = CilAnalysis.MethodToText(entryPoint);
                
                AssertThat.IsMatch(str, new Text[] { 
                    ".method", Text.Any, "Main", Text.Any, "{", Text.Any, 
                    ".entrypoint", Text.Any, "}", Text.Any
                });
            }
        }

        [TestMethod]
        public void Test_EntryPoint_Library()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                MethodInfo entryPoint = ass.EntryPoint;
                Assert.IsNull(entryPoint);
            }
        }
    }
}
