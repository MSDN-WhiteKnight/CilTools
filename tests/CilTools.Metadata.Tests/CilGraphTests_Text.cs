/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
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
        public void Test_CilGraph_Attributes()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase mi = t.GetMember("AttributeTest")[0] as MethodBase;
                CilGraph graph = CilGraph.Create(mi);

                string str = graph.ToText();

                AssertThat.IsMatch(str, new MatchElement[] {
                ".method", MatchElement.Any,
                ".custom", MatchElement.Any,
                "instance", MatchElement.Any,
                "void", MatchElement.Any,
                "System.STAThreadAttribute", MatchElement.Any,
                ".ctor", MatchElement.Any,
                "(", MatchElement.Any,
                "01 00 00 00", MatchElement.Any,
                ")", MatchElement.Any
                });

                AssertThat.IsMatch(str, new MatchElement[] {
                ".method", MatchElement.Any,
                ".custom", MatchElement.Any,
                "instance", MatchElement.Any,
                "void", MatchElement.Any,
                "CilTools.Tests.Common.MyAttribute", MatchElement.Any,
                ".ctor", MatchElement.Any,
                "(", MatchElement.Any,
                "int32", MatchElement.Any,
                ")", MatchElement.Any,
                "=", MatchElement.Any,
                "(", MatchElement.Any,
                "01 00 01 00 00 00 00 00", MatchElement.Any,
                ")", MatchElement.Any,
                });
            }
        }

        [TestMethod]
        public void Test_CilGraph_PInvoke()
        {
            AssemblyReader reader = new AssemblyReader();

            using (reader)
            {
                Assembly ass = reader.LoadFrom(typeof(SampleMethods).Assembly.Location);
                Type t = ass.GetType("CilTools.Tests.Common.SampleMethods");
                MethodBase mi = t.GetMember("ShowWindow")[0] as MethodBase;
                CilGraph graph = CilGraph.Create(mi);
                string str = graph.ToText();

                AssertThat.IsMatch(str, new MatchElement[] {
                ".method", MatchElement.Any,
                "static", MatchElement.Any,
                "pinvokeimpl", MatchElement.Any,
                "(", MatchElement.Any,
                "\"user32.dll\"", MatchElement.Any,
                "lasterr", MatchElement.Any,
                ")", MatchElement.Any,
                "bool", MatchElement.Any,
                "ShowWindow", MatchElement.Any,
                "native int", MatchElement.Any,
                "int32", MatchElement.Any,
                "cil", MatchElement.Any,
                "managed", MatchElement.Any
                });
            }

            /*.method public hidebysig static pinvokeimpl("user32.dll" lasterr winapi) 
            bool ShowWindow(native int hWnd,
                             int32 nCmdShow) cil managed preservesig*/
        }
    }
}
