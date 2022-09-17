/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Reflection;
using CilView.Common;
using CilView.Core.Syntax;

namespace CilView.SourceCode.VisualBasic
{
    class VbDecompiler : Decompiler
    {
        public VbDecompiler(MethodBase method) : base(method)
        {

        }

        static string GetTypeString(Type t)
        {
            if (t == null) return string.Empty;

            if (t.IsArray && t.GetArrayRank() == 1)
            {
                return GetTypeString(t.GetElementType()) + "()";
            }

            if (t.IsGenericType)
            {
                StringBuilder sb = new StringBuilder(100);
                sb.Append(GetGenericDefinitionName(t.Name));
                sb.Append('(');
                sb.Append("Of ");
                Type[] args = t.GetGenericArguments();

                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) sb.Append(", ");

                    sb.Append(GetTypeString(args[i]));
                }

                sb.Append(')');
                return sb.ToString();
            }

            //built-in types
            if (Utils.TypeEquals(t, typeof(int))) return "Integer";
            else if (Utils.TypeEquals(t, typeof(uint))) return "UInteger";
            else if (Utils.TypeEquals(t, typeof(long))) return "Long";
            else if (Utils.TypeEquals(t, typeof(ulong))) return "ULong";
            else if (Utils.TypeEquals(t, typeof(short))) return "Short";
            else if (Utils.TypeEquals(t, typeof(ushort))) return "UShort";
            else if (Utils.TypeEquals(t, typeof(DateTime))) return "Date";
            
            return t.Name;
        }

        string GetMethodSigString()
        {
            MethodBase m = this._method;
            StringBuilder sb = new StringBuilder(500);

            if (m.IsPublic) sb.Append("Public ");
            else if (m.IsFamily) sb.Append("Protected ");
            else if (m.IsAssembly) sb.Append("Friend ");
            else if (m.IsPrivate) sb.Append("Private ");

            if (m.IsStatic) sb.Append("Shared ");
            if (m.IsAbstract) sb.Append("MustOverride ");

            string rettype = string.Empty;
            bool isFunc = false;

            if (m is ICustomMethod)
            {
                ICustomMethod cm = (ICustomMethod)m;
                Type t = cm.ReturnType;

                if (t != null)
                {
                    if (Utils.StringEquals(t.FullName, "System.Void"))
                    {
                        rettype = "void";
                    }
                    else
                    {
                        rettype = GetTypeString(t);
                        isFunc = true;
                    }
                }
            }

            if(isFunc) sb.Append("Function ");
            else sb.Append("Sub ");

            sb.Append(m.Name);

            if (m.IsGenericMethod)
            {
                sb.Append("(Of ");

                Type[] args = m.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) sb.Append(", ");

                    sb.Append(args[i].Name);
                }

                sb.Append(')');
                sb.Append(' ');
            }

            ParameterInfo[] pars = m.GetParameters();
            sb.Append('(');

            for (int i = 0; i < pars.Length; i++)
            {
                if (i >= 1) sb.Append(", ");

                string parname = pars[i].Name;

                if (string.IsNullOrEmpty(parname))
                {
                    parname = "par" + (i + 1).ToString();
                }

                sb.Append(parname);
                sb.Append(" As ");
                sb.Append(GetTypeString(pars[i].ParameterType));
            }

            sb.Append(')');

            if (isFunc)
            {
                sb.Append(" As ");
                sb.Append(rettype);
            }
            
            return sb.ToString();
        }

        public override IEnumerable<SourceToken> GetMethodSigTokens()
        {
            yield return new SourceToken(this.GetMethodSigString(), TokenKind.Unknown);
        }
    }
}
