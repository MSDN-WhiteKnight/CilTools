/* CilTools.Metadata tests
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Metadata.Tests.TestData
{
    public class SampleType
    {
        public static string PublicStaticField;
        public string PublicInstanceField;
        private static string PrivateStaticField;
        private string PrivateInstanceField;
        public string PublicProperty { get; set; }

        public static void PublicStaticMethod()
        {
            PrivateStaticField = string.Empty;
            Console.WriteLine(PrivateStaticField);
            PrivateStaticMethod();
        }
        public void PublicInstanceMethod()
        {
            PrivateInstanceField = string.Empty;
            Console.WriteLine(PrivateInstanceField);
            PrivateInstanceMethod();
        }

        private static void PrivateStaticMethod() { }
        private void PrivateInstanceMethod() { }
    }
}
