/* CilTools.Metadata tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;
using CilTools.Reflection;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class ReflectionTests
    {
        [TestMethod]
        public void Test_PInvoke()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                CustomMethod mi = t.GetMember("ShowWindow")[0] as CustomMethod;

                Assert.IsTrue(
                    mi.Attributes.HasFlag(MethodAttributes.PinvokeImpl),
                    "ShowWindow should have PinvokeImpl attribute"
                    );

                PInvokeParams pars = mi.GetPInvokeParams();

                Assert.AreEqual("user32.dll", pars.ModuleName);
                Assert.IsTrue(pars.SetLastError, "SetLastError should be true for ShowWindow");
            }
        }
    }
}
