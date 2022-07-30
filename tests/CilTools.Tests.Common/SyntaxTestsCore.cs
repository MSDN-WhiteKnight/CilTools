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
    }
}
