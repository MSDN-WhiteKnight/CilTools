/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Tests.Common.TestData
{
    public interface ITest
    {
        int Foo();
        void Bar(string s, object o);
    }

    public class InterfacesSampleType : ITest, IComparable
    {
        public void Bar(string s, object o) { }

        public int CompareTo(object obj) { return 0; }

        public int Foo() { throw new NotImplementedException(); }
    }
}
