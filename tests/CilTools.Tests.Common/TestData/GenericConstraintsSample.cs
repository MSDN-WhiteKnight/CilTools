/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Tests.Common.TestData
{
    public static class GenericConstraintsSample<T> where T : struct
    {
        public static T Sum(T x, T y)
        {
            if (typeof(T) == typeof(int))
            {
                int res = (Convert.ToInt32(x) + Convert.ToInt32(y));
                return (T)Convert.ChangeType(res, typeof(int));
            }
            else if (typeof(T) == typeof(float))
            {
                float res = (Convert.ToSingle(x) + Convert.ToSingle(y));
                return (T)Convert.ChangeType(res, typeof(int));
            }
            else throw new NotSupportedException();
        }
    }
}
