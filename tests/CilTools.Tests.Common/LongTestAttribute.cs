/* CIL Tools tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Tests.Common
{
    [AttributeUsage(AttributeTargets.Method|AttributeTargets.Class)]
    public class LongTestAttribute:Attribute
    {
    }
}
