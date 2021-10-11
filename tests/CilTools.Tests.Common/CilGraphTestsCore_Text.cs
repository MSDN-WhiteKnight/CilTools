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
            AssertThat.IsMatch(str, new MatchElement[] { ".method", MatchElement.Any, "public" });
            AssertThat.IsMatch(str, new MatchElement[] { ".method", MatchElement.Any, "static" });

            AssertThat.IsMatch(str, new MatchElement[] { 
                ".method", MatchElement.Any, "void", MatchElement.Any, 
                "PrintHelloWorld", MatchElement.Any, 
                "cil", MatchElement.Any, "managed", MatchElement.Any
            });

            Assert.IsFalse(str.Contains("call"), "The result of CilGraph.ToString should not contain instructions");
        }
    
        public static void Test_CilGraph_EmptyString(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);

            //Test correct empty string output
            string str = graph.ToText();

            AssertThat.IsMatch(str, new MatchElement[] { 
                MatchElement.Any, "ldstr", MatchElement.Any, "\"\"", MatchElement.Any 
            });
        }
        
        public static void Test_CilGraph_OptionalParams(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);

            //Test correct empty string output
            string str = graph.ToText();
            
            AssertThat.IsMatch(str, new MatchElement[] { 
                MatchElement.Any, ".method",
                MatchElement.Any, "[opt]",MatchElement.Any, "string",
                MatchElement.Any, "[opt]",MatchElement.Any, "int32",
                MatchElement.Any, ".param", MatchElement.Any, "[1]",
                MatchElement.Any, "\"\"", 
                MatchElement.Any, ".param", MatchElement.Any, "[2]",
                MatchElement.Any, "int32(0)", 
                MatchElement.Any 
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

            AssertThat.IsMatch(str, new MatchElement[] { 
                MatchElement.Any, ".locals",MatchElement.Any, "(",
                MatchElement.Any, "MyPoint",MatchElement.Any, ")",
                MatchElement.Any 
            });
        }

        public static void Test_CilGraph_ImplRuntime(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            string str = graph.ToString();

            //.method   public hidebysig newslot virtual instance !0 Invoke() runtime managed

            AssertThat.IsMatch(str, new MatchElement[] {
                MatchElement.Any, ".method",MatchElement.Any,
                "!",MatchElement.Any,
                "Invoke",MatchElement.Any,
                "(", MatchElement.Any,")",MatchElement.Any
                ,"runtime",MatchElement.Any
                ,"managed",MatchElement.Any
            });
        }
    }
}
