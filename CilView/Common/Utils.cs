/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.Common
{
    public static class Utils
    {
        public static bool StringEquals(string left, string right)
        {
            return String.Equals(left, right, StringComparison.InvariantCulture);
        }

        public static bool PathEquals(string left, string right)
        {
            if (left == null)
            {
                if (right == null) return true;
                else return false;
            }

            if (right == null) return false;

            if (left.EndsWith("\\") || left.EndsWith("/"))
            {
                left = left.Substring(0, left.Length - 1);
            }

            if (right.EndsWith("\\") || right.EndsWith("/"))
            {
                right = right.Substring(0, right.Length - 1);
            }

            left = left.ToLower();
            right = right.ToLower();
            return StringEquals(left,right);
        }
    }
}
