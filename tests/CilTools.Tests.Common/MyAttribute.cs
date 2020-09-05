/* CIL Tools
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Tests.Common
{
    public class MyAttribute : Attribute
    {
        int _x;

        public MyAttribute(int x) { this._x = x; }

        public override string ToString()
        {
            return "MyAttribute: " + this._x.ToString();
        }
    }
}
