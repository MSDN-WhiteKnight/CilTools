/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Reflection;
using CilView.Common;

namespace CilView.SourceCode
{
    /// <summary>
    /// Base class for minimalistic decompiler implementations that produce method signatures in high-level .NET 
    /// languages. This is needed to support "Show source (method)" feature, as PDB seuence points only point to 
    /// method body source lines.
    /// </summary>
    public abstract class Decompiler
    {
        protected MethodBase _method;

        protected Decompiler(MethodBase method)
        {
            this._method = method;
        }

        /// <summary>
        /// Returns a collection of tokens that represents decompiled method signature code in target language
        /// </summary>
        public abstract IEnumerable<SourceToken> GetMethodSigTokens();

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
            else if (ext.Equals(".vb"))
            {
                //VB.NET don't need decompiler because symbols point to the method signature
                return new NullDecompiler(m);
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
            IEnumerable<SourceToken> tokens = d.GetMethodSigTokens();
            StringBuilder sb = new StringBuilder(500);

            foreach (SourceToken tok in tokens) sb.Append(tok.ToString());

            return sb.ToString();
        }

        protected static SourceToken ProcessCommonTypes(Type t)
        {
            //built-in types common between C# and C++/CLI
            if (Utils.TypeEquals(t, typeof(bool)))        return new SourceToken("bool", SourceTokenKind.Keyword);
            else if (Utils.TypeEquals(t, typeof(int)))    return new SourceToken("int", SourceTokenKind.Keyword);
            else if (Utils.TypeEquals(t, typeof(short)))  return new SourceToken("short", SourceTokenKind.Keyword);
            else if (Utils.TypeEquals(t, typeof(float)))  return new SourceToken("float", SourceTokenKind.Keyword);
            else if (Utils.TypeEquals(t, typeof(double))) return new SourceToken("double", SourceTokenKind.Keyword);
            else return null;
        }
        
        protected static string GetGenericDefinitionName(string typeName)
        {
            int i = typeName.IndexOf('`');

            if (i <= 0) return typeName;
            else return typeName.Substring(0, i);
        }

        protected static Type GetReturnType(MethodBase m)
        {
            if (m is ICustomMethod)
            {
                ICustomMethod cm = (ICustomMethod)m;
                return cm.ReturnType;
            }
            else if (m is MethodInfo)
            {
                MethodInfo mi = (MethodInfo)m;
                return mi.ReturnType;
            }
            else return null;
        }

        private class NullDecompiler : Decompiler
        {
            public NullDecompiler(MethodBase method) : base(method) { }
            
            public override IEnumerable<SourceToken> GetMethodSigTokens()
            {
                return new SourceToken[0];
            }
        }
    }
}
