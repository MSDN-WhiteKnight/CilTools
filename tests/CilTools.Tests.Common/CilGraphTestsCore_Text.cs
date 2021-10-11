/* CIL Tools
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using CilTools.BytecodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Tests.Common
{
    public class CilGraphTestsCore_Text
    {
        public static void Test_CilGraph_ToString(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);

            //Test that ToString returns signature
            string str = graph.ToString();
            AssertThat.IsMatch(str, new Text[] { ".method", Text.Any, "public" });
            AssertThat.IsMatch(str, new Text[] { ".method", Text.Any, "static" });

            AssertThat.IsMatch(str, new Text[] { 
                ".method", Text.Any, "void", Text.Any, 
                "PrintHelloWorld", Text.Any, 
                "cil", Text.Any, "managed", Text.Any
            });

            Assert.IsFalse(str.Contains("call"), "The result of CilGraph.ToString should not contain instructions");
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
            CilGraph graph = CilGraph.Create(mi);

            StringBuilder sb = new StringBuilder(100);
            StringWriter wr = new StringWriter(sb);
            graph.PrintHeader(wr);
            wr.Flush();
            string str = sb.ToString();

            AssertThat.IsMatch(str, new Text[] { 
                Text.Any, ".locals",Text.Any, "(",
                Text.Any, "MyPoint",Text.Any, ")",
                Text.Any 
            });
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
