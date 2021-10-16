/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Reflection;
using CilTools.Tests.Common;
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
            CustomMethod m = null;

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                m = t.GetMember("PrintHelloWorld")[0] as CustomMethod;

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
            CustomMethod m = null;

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(TestType).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.TestType");
                m = t.GetMember(".ctor", Utils.AllMembers())[0] as CustomMethod;

                Assert.AreEqual(".ctor", m.Name);
                Assert.AreEqual(MemberTypes.Constructor, m.MemberType);
                Assert.IsNull(m.ReturnType);
            }
        }
    }
}
