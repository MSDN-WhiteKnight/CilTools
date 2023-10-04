/* CIL Tools
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.Tests.Visualization
{
    public class SampleType
    {
        public float X;
        public float Y;

        public float CalcSum() { return this.X + this.Y; }
    }

    public class ProvidersSampleType
    {
        public static void PrintSum(int x, int y)
        {
            SampleType st = new SampleType();
            st.X = x;
            st.Y = y;
            Console.WriteLine(st.CalcSum());
        }
    }
}
