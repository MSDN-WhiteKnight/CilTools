/* CIL Tools tests
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using CilTools.Tests.Common;

namespace CilTools.Runtime.Tests
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("Initializing...");
                ClrTests.StartSession();
                return LongTestsRunner.Run(typeof(Program).Assembly);
            }
            finally
            {
                ClrTests.CloseSession();
            }
        }
    }
}
