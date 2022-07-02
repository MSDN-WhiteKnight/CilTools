/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Tests.Common
{
    public class CilGraphTestsCore_Text
    {
        public static void Test_CilGraph_ToString(MethodBase mi)
        {
            //Test that CilGraph.ToString returns signature
            const string expected = ".method public hidebysig static void PrintHelloWorld() cil managed";

            CilGraph graph = CilGraph.Create(mi);
            string str = graph.ToString();
            AssertThat.AreLexicallyEqual(expected, str);
        }
    
        public static void Test_CilGraph_EmptyString(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);

            //Test correct empty string output
            string str = graph.ToText();

            AssertThat.IsMatch(str, new Text[] { 
                Text.Any, "ldstr", Text.Any, "\"\"", Text.Any 
            });
        }
        
        public static void Test_CilGraph_OptionalParams(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);

            //Test correct empty string output
            string str = graph.ToText();
            
            AssertThat.IsMatch(str, new Text[] { 
                Text.Any, ".method",
                Text.Any, "[opt]",Text.Any, "string",
                Text.Any, "[opt]",Text.Any, "int32",
                Text.Any, ".param", Text.Any, "[1]",
                Text.Any, "\"\"", 
                Text.Any, ".param", Text.Any, "[2]",
                Text.Any, "int32(0)", 
                Text.Any 
            });
        }

        public static void Test_CilGraph_Locals(MethodBase mi)
        {
            const string expected = @".maxstack 2
.locals init (class [CilTools.Tests.Common]CilTools.Tests.Common.MyPoint V_0,
    class [CilTools.Tests.Common]CilTools.Tests.Common.MyPoint V_1)";

            CilGraph graph = CilGraph.Create(mi);

            StringBuilder sb = new StringBuilder(100);
            StringWriter wr = new StringWriter(sb);
            graph.PrintHeader(wr);
            wr.Flush();
            string str = sb.ToString();

            AssertThat.AreLexicallyEqual(expected, str);
        }

        public static void Test_CilGraph_ImplRuntime(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            string str = graph.ToString();

            //.method   public hidebysig newslot virtual instance !0 Invoke() runtime managed

            AssertThat.IsMatch(str, new Text[] {
                Text.Any, ".method",Text.Any,
                "!",Text.Any,
                "Invoke",Text.Any,
                "(", Text.Any,")",Text.Any
                ,"runtime",Text.Any
                ,"managed",Text.Any
            });
        }
    }
}
