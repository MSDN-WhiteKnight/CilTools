/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using CilTools.Reflection;

namespace CilTools.BytecodeAnalysis
{
    /// <summary>
    /// Provides tools to help investigate errors occuring in library methods.
    /// </summary>
    public static class Diagnostics
    {
        /// <summary>
        /// Raised when error occurs in one of the methods in the library
        /// </summary>
        public static event EventHandler<CilErrorEventArgs> Error;

        internal static void OnError(object sender, CilErrorEventArgs e)
        {
            EventHandler<CilErrorEventArgs> handler = Error;
            if (handler != null)
            {
                handler(sender, e);
            }
        }
    }
}
