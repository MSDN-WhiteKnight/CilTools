/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Reflection;
using CilTools.Syntax;
using CilTools.Tests.Common;
using CilTools.Tests.Common.TestData;
using CilTools.Tests.Common.TextUtils;
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

        [TestMethod]
        [WorkItem(53)]
        public void Test_GenericType_Constraints()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(GenericConstraintsSample<>).Assembly.Location);
                Type t = ass.GetType(typeof(GenericConstraintsSample<>).FullName);
                Type[] args = t.GetGenericArguments();
                Type tParam = args[0];
                Type[] cons = tParam.GetGenericParameterConstraints();
                GenericParameterAttributes expected = GenericParameterAttributes.NotNullableValueTypeConstraint |
                    GenericParameterAttributes.DefaultConstructorConstraint;

                Assert.AreEqual(expected, tParam.GenericParameterAttributes);
                Assert.AreEqual(1, cons.Length);
                Assert.AreEqual("System.ValueType", cons[0].FullName);
            }
        }

        [TestMethod]
        [WorkItem(53)]
        public void Test_GenericType_Constraints_None()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(IList<>).Assembly.Location);
                Type t = ass.GetType(typename);
                Type[] args = t.GetGenericArguments();
                Type tParam = args[0];
                Type[] cons = tParam.GetGenericParameterConstraints();
                GenericParameterAttributes expected = GenericParameterAttributes.None;

                Assert.AreEqual(expected, tParam.GenericParameterAttributes);
                Assert.AreEqual(0, cons.Length);
            }
        }

        [TestMethod]
        [WorkItem(53)]
        public void Test_GenericType_ConstraintsSyntax()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(GenericConstraintsSample<>).Assembly.Location);
                Type t = ass.GetType(typeof(GenericConstraintsSample<>).FullName);
                IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t);
                string str = Utils.SyntaxToString(nodes);

                AssertThat.IsMatch(str, new Text[] {
                  ".class", Text.Any, "public", Text.Any, "GenericConstraintsSample", Text.Any,
                  "<", Text.Any, "valuetype", Text.Any, ".ctor", Text.Any, "(", Text.Any,
                  "System.ValueType", Text.Any,")", Text.Any, "T", Text.Any, ">", Text.Any,
                  "{", Text.Any, "}", Text.Any
                });
            }
        }

        [TestMethod]
        [WorkItem(53)]
        public void Test_GenericType_FlagsSyntax()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(Action<>).Assembly.Location);
                Type t = ass.GetType(typeof(Action<>).FullName);
                IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t);
                string str = Utils.SyntaxToString(nodes);

                AssertThat.IsMatch(str, new Text[] {
                  ".class", Text.Any, "public", Text.Any, "Action", Text.Any,
                  "<", Text.Any, "-", Text.Any, "T", Text.Any, ">", Text.Any,
                  "{", Text.Any, "}", Text.Any
                });
            }
        }
    }
}
