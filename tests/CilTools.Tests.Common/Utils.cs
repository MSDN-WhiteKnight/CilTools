/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace CilTools.Tests.Common
{
    public static class Utils
    {
        /// <summary>
        /// Provides <see cref="BindingFlags"/> mask that matches all members 
        /// (public and non-public, static and instance)
        /// </summary>
        public static BindingFlags AllMembers()
        {
            return BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance;
        }
    }
}
