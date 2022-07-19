/* CilTools.Metadata tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Tests.Common;
using CilTools.Tests.Common.TextUtils;
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

                AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any,
                ".custom", Text.Any,
                "instance", Text.Any,
                "void", Text.Any,
                "System.STAThreadAttribute", Text.Any,
                ".ctor", Text.Any,
                "(", Text.Any,
                "01 00 00 00", Text.Any,
                ")", Text.Any
                });

                AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any,
                ".custom", Text.Any,
                "instance", Text.Any,
                "void", Text.Any,
                "CilTools.Tests.Common.MyAttribute", Text.Any,
                ".ctor", Text.Any,
                "(", Text.Any,
                "int32", Text.Any,
                ")", Text.Any,
                "=", Text.Any,
                "(", Text.Any,
                "01 00 01 00 00 00 00 00", Text.Any,
                ")", Text.Any,
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

                AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any,
                "static", Text.Any,
                "pinvokeimpl", Text.Any,
                "(", Text.Any,
                "\"user32.dll\"", Text.Any,
                "lasterr", Text.Any,
                ")", Text.Any,
                "bool", Text.Any,
                "ShowWindow", Text.Any,
                "native int", Text.Any,
                "int32", Text.Any,
                "cil", Text.Any,
                "managed", Text.Any
                });
            }

            /*.method public hidebysig static pinvokeimpl("user32.dll" lasterr winapi) 
            bool ShowWindow(native int hWnd,
                             int32 nCmdShow) cil managed preservesig*/
        }
    }
}
