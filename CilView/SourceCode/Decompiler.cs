/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilView.Common;

namespace CilView.SourceCode
{
    /// <summary>
    /// Base class for minimalistic decompiler implementations that produce method signatures in high-level .NET 
    /// languages. This is needed to support "Show source (method)" feature, as PDB seuence points only point to 
    /// method body source lines.
    /// </summary>
    abstract class Decompiler
    {
        protected MethodBase _method;

        protected Decompiler(MethodBase method)
        {
            this._method = method;
        }

        /// <summary>
        /// Returns a string that contains decompiled method signature code in target language
        /// </summary>
        public abstract string GetMethodSigString();

        public static bool IsCppExtension(string ext)
        {
            return ext == ".cpp" || ext == ".c" || ext == ".h" || ext == string.Empty;
        }

        static Decompiler Create(string ext,MethodBase m)
        {
            if (ext == null) ext = string.Empty;

            ext = ext.Trim();

            if (IsCppExtension(ext))
            {
                return new CppDecompiler(m);
            }
            else
            {
                return new CsharpDecompiler(m);
            }
        }

        /// <summary>
        /// Returns a string that contains a decompiled signature of the specified method in a language 
        /// identified by a file extension
        /// </summary>
        /// <param name="ext">Code file extension (with dot), or empty string</param>
        /// <param name="m">Method to get the decompiled signature</param>
        public static string DecompileMethodSignature(string ext, MethodBase m)
        {
            Decompiler d = Create(ext, m);
            return d.GetMethodSigString();
        }

        protected static string ProcessCommonTypes(Type t)
        {
            //built-in types common between C# and C++/CLI
            if (Utils.TypeEquals(t, typeof(bool)))        return "bool";
            else if (Utils.TypeEquals(t, typeof(int)))    return "int";
            else if (Utils.TypeEquals(t, typeof(short)))  return "short";
            else if (Utils.TypeEquals(t, typeof(float)))  return "float";
            else if (Utils.TypeEquals(t, typeof(double))) return "double";
            else return null;
        }
    }
}
