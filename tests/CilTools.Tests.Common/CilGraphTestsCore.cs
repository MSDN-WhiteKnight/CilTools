/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CilTools.Tests.Common.TextUtils;

namespace CilTools.Tests.Common
{
    public class CilGraphTestsCore
    {
        public static void Assert_CilGraph_HelloWorld(string str) 
        {
            AssertThat.IsMatch(str, new Text[] { ".method", Text.Any, "public" });
            AssertThat.IsMatch(str, new Text[] { ".method", Text.Any, "static" });

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any, "void", Text.Any,
                "PrintHelloWorld", Text.Any,
                "cil", Text.Any, "managed", Text.Any,
                "{", Text.Any,
                "ldstr", Text.Any, "\"Hello, World\"", Text.Any,
                "call", Text.Any,
                "void", Text.Any, "System.Console::WriteLine", Text.Any,
                "ret", Text.Any,
                "}"
            });
        }

        public static void Assert_CilGraph_Loop(string str)
        {
            AssertThat.IsMatch(str, new Text[] { ".method", Text.Any, "public" });
            AssertThat.IsMatch(str, new Text[] { ".method", Text.Any, "static" });

            AssertThat.IsMatch(str, new Text[] {
                ".method", Text.Any, "void", Text.Any,
                "PrintTenNumbers", Text.Any,
                "cil", Text.Any, "managed", Text.Any,
                "{", Text.Any,
                ".locals", Text.Any, "int32", Text.Any,
                "call", Text.Any,
                "void", Text.Any, "System.Console::WriteLine", Text.Any,
                "ret", Text.Any,
                "}"
            });

            AssertThat.IsMatch(str, new Text[] { "IL_", Text.Any, ":" });
        }
    }
}
