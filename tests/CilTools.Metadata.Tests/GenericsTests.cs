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
    public class GenericsTests
    {
        const string typename = "System.Collections.Generic.IList`1";

        [TestMethod]
        public void Test_GenericTypeParams_Name()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(IList<>).Assembly.Location);
                Type t = ass.GetType(typename);
                MethodBase mi = t.GetMember("IndexOf")[0] as MethodBase;
                ParameterInfo[] pars = mi.GetParameters();
                Type parameterType = pars[0].ParameterType;
                Assert.IsTrue(parameterType.IsGenericParameter);
                Assert.AreEqual("T", parameterType.Name);
            }
        }

        [TestMethod]
        public void Test_GenericTypeParams_DeclaringType()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(IList<>).Assembly.Location);
                Type t = ass.GetType(typename);
                MethodBase mi = t.GetMember("IndexOf")[0] as MethodBase;
                ParameterInfo[] pars = mi.GetParameters();
                Type parameterType = pars[0].ParameterType;
                Assert.IsTrue(parameterType.IsGenericParameter);
                Assert.AreEqual(typename, parameterType.DeclaringType.FullName);
                Assert.IsNull(parameterType.DeclaringMethod);
            }
        }
    }
}
