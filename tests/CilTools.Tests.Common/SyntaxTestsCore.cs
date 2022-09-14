/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using CilTools.Tests.Common.TextUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Tests.Common
{
    public static class SyntaxTestsCore
    {
        public static void SampleMethods_AssertTypeSyntax(string s)
        {
            AssertThat.IsMatch(s, new Text[] {
                ".class", Text.Any,"public", Text.Any,"abstract", Text.Any,"CilTools.Tests.Common.SampleMethods", Text.Any,
                "{", Text.Any,
                ".field", Text.Any,"public", Text.Any,"static", Text.Any,"int32", Text.Any,"Foo", Text.Any,
                "}", Text.Any
            });

            AssertThat.IsMatch(s, new Text[] {
                ".class", Text.Any,
                ".field", Text.Any,"public", Text.Any,"static", Text.Any,"int32", Text.Any,"counter", Text.Any
            });

            AssertThat.IsMatch(s, new Text[] {
                ".class", Text.Any,
                ".field", Text.Any,"public", Text.Any,"static", Text.Any,"int32", Text.Any,"f", Text.Any
            });

            Assert.IsFalse(s.Contains(".method"));
        }

        public static void VerifyAssemblyManifestString(string str, string ver)
        {
            // Verify disassembled assembly manifest for CilTools.Tests.Common

            AssertThat.IsMatch(str, new Text[] {
                ".assembly", Text.Any, "extern", Text.Any, "CilTools.BytecodeAnalysis", Text.Any,
                "{", Text.Any, ".ver", Text.Any, ver, Text.Any, "}", Text.Any,
                ".assembly", Text.Any, "CilTools.Tests.Common", Text.Any, "{", Text.Any,
                ".custom", Text.Any, "System.Reflection.AssemblyTitleAttribute::.ctor(string)", Text.Any,
                ".ver", Text.Any, ver, Text.Any, "}", Text.Any,
                ".module", Text.Any, "CilTools.Tests.Common.dll", Text.Any,
                ".custom", Text.Any, "System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 )", Text.Any,
            });
        }
    }
}
