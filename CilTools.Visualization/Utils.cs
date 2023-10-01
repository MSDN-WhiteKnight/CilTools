/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CilTools.Visualization
{
    internal static class Utils
    {
        public static bool StrEquals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        public static int CompareTypes(Type x, Type y)
        {
            string s1 = x.FullName;
            string s2 = y.FullName;
            return string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsTypeInAssembly(Type t, Assembly ass)
        {
            if (t == null || ass == null) return false;

            Assembly typeAssembly = t.Assembly;

            if (typeAssembly == null) return false;

            string name1 = typeAssembly.GetName().Name;
            string name2 = ass.GetName().Name;
            return string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
