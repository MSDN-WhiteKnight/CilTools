/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Tests.Common;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class SyntaxTests
    {
        [TestMethod]
        public void Test_ToSyntaxTree()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase mi = t.GetMember("PrintHelloWorld")[0] as MethodBase;
                SyntaxTestsCore.Test_ToSyntaxTree(mi);
            }
        }
    }
}