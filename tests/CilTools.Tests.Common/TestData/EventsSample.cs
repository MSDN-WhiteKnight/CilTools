/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Tests.Common.TestData
{
    public class EventsSample
    {
        public event Action A;

        [My(4)]
        public static event EventHandler<EventArgs> B;

        event Action<int> C
        {
            add { }
            remove { }
        }

        public void Dummy()
        {
            A();
            B(null, null);
            C += null;
        }
    }

    public class EventsSampleDerived : EventsSample
    {
        public event Action<string> D;

        public void Dummy2()
        {
            D("Hello");
        }
    }
}
