/* CilTools.Metadata tests
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Metadata.Tests
{
    [TestClass]
    public class CilGraphTests_Text
    {
        [TestMethod]
        public void Test_CilGraph_ToString()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("PrintHelloWorld")[0] as MethodBase;
            CilGraphTestsCore_Text.Test_CilGraph_ToString(mi);
        }

        [TestMethod]
        public void Test_CilGraph_EmptyString()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("TestEmptyString")[0] as MethodBase;
            CilGraphTestsCore_Text.Test_CilGraph_EmptyString(mi);
        }

        [TestMethod]
        public void Test_CilGraph_OptionalParams()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("TestOptionalParams")[0] as MethodBase;
            CilGraphTestsCore_Text.Test_CilGraph_OptionalParams(mi);
        }

        [TestMethod]
        public void Test_CilGraph_Attributes()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("AttributeTest")[0] as MethodBase;
            CilGraph graph = CilGraph.Create(mi);

            string str = graph.ToText();
            
            AssertThat.IsMatch(str, new MatchElement[] { 
                new Literal(".method"), MatchElement.Any, 
                new Literal(".custom"), MatchElement.Any,
                new Literal("instance"), MatchElement.Any,
                new Literal("void"), MatchElement.Any,
                new Literal("System.STAThreadAttribute"), MatchElement.Any,
                new Literal(".ctor"), MatchElement.Any,
                new Literal("("), MatchElement.Any,
                new Literal("01 00 00 00"), MatchElement.Any,
                new Literal(")"), MatchElement.Any
            });

            AssertThat.IsMatch(str, new MatchElement[] {
                new Literal(".method"), MatchElement.Any,
                new Literal(".custom"), MatchElement.Any,
                new Literal("instance"), MatchElement.Any,
                new Literal("void"), MatchElement.Any,
                new Literal("CilTools.Tests.Common.MyAttribute"), MatchElement.Any,
                new Literal(".ctor"), MatchElement.Any,
                new Literal("("), MatchElement.Any,
                new Literal("int32"), MatchElement.Any,
                new Literal(")"), MatchElement.Any,
                new Literal("="), MatchElement.Any,
                new Literal("("), MatchElement.Any,
                new Literal("01 00 01 00 00 00 00 00"), MatchElement.Any,
                new Literal(")"), MatchElement.Any,
            });
        }

#if DEBUG
        [TestMethod]
        public void Test_CilGraph_Locals()
        {
            Assembly ass = MetadataLoader.Load(typeof(SampleMethods).Assembly.Location);
            Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
            MethodBase mi = t.GetMember("CreatePoint")[0] as MethodBase;
            CilGraphTestsCore_Text.Test_CilGraph_Locals(mi);
        }
#endif
    }
}
