/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.Core.Reflection
{
    public class MethodExecutionResults
    {
        public object ReturnValue { get; set; }

        public Exception ExceptionObject { get; set; }

        public bool IsTimedOut { get; set; }

        public TimeSpan Duration { get; set; }
    }
}
