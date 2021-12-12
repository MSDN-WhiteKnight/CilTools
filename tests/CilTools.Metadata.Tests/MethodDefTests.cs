/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.Reflection;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class MethodDefTests
    {
        const string typename = "CilTools.Tests.Common.SampleMethods";

        [TestMethod]
        public void Test_MethodDef_DeclaringType()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typename);
                MethodBase m = t.GetMember("PrintHelloWorld")[0] as MethodBase;
                Type tDecl = m.DeclaringType;
                Assert.AreEqual(typename, tDecl.FullName);
                Assert.IsTrue(tDecl.IsPublic);
                Assert.IsTrue(tDecl.IsClass);
                Assert.AreSame(t, tDecl);
            }
        }

        [TestMethod]
        public void Test_MethodDef_GetExceptionBlocks()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType(typename);
                ICustomMethod m = t.GetMember("DivideNumbers")[0] as ICustomMethod;
                ExceptionBlock[] blocks=m.GetExceptionBlocks();
                Assert.AreEqual(2, blocks.Length);

                ExceptionBlock catchBlock;
                catchBlock = blocks.Where(x => x.Flags == ExceptionHandlingClauseOptions.Clause).First();
                Type catchType = catchBlock.CatchType;
                Assert.AreEqual("DivideByZeroException", catchType.Name);

                AssertThat.HasOnlyOneMatch(blocks, x => x.Flags == ExceptionHandlingClauseOptions.Finally);
            }
        }
    }
}
