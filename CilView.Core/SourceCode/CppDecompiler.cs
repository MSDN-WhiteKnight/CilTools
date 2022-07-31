/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Reflection;
using CilView.Common;

namespace CilView.SourceCode
{
    class CppDecompiler : Decompiler
    {
        public CppDecompiler(MethodBase method) : base(method)
        {

        }

        static string GetTypeString(Type t)
        {
            if (t == null) return string.Empty;

            if (t.IsArray && t.GetArrayRank() == 1)
            {
                StringBuilder sb = new StringBuilder(100);
                sb.Append("array <");
                sb.Append(GetTypeString(t.GetElementType()));
                sb.Append("> ^");
                return sb.ToString();
            }

            if (t.IsGenericType)
            {
                StringBuilder sb = new StringBuilder(100);
                sb.Append(GetGenericDefinitionName(t.Name));
                sb.Append('<');
                Type[] args = t.GetGenericArguments();

                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) sb.Append(", ");

                    sb.Append(GetTypeString(args[i]));
                }

                sb.Append("> ^");
                return sb.ToString();
            }

            //reference types are represented by handles in C++/CLI
            if (t.IsClass || t.IsInterface) return t.Name + " ^";

            //process built-in types
            string s = ProcessCommonTypes(t);

            if (s != null) return s;

            if (Utils.TypeEquals(t, typeof(uint)))        return "unsigned int";
            else if (Utils.TypeEquals(t, typeof(ushort))) return "unsigned short";
            else if (Utils.TypeEquals(t, typeof(byte)))   return "unsigned char";
            else if (Utils.TypeEquals(t, typeof(sbyte)))  return "signed char";
            else if (Utils.TypeEquals(t, typeof(char)))   return "wchar_t";

            return t.Name;
        }

        public override string GetMethodSigString()
        {
            MethodBase m = this._method;
            StringBuilder sb = new StringBuilder(500);
            ParameterInfo[] pars = m.GetParameters();

            //global functions don't have access modifiers
            bool isGlobalFunc = m.DeclaringType == null || Utils.StringEquals(m.DeclaringType.Name, "<Module>");

            if (!isGlobalFunc)
            {
                if (m.IsPublic) sb.Append("public: ");
                else if (m.IsFamily) sb.Append("protected: ");
                else if (m.IsAssembly) sb.Append("internal: ");
            }
            
            if (m.IsGenericMethod)
            {
                sb.Append("generic <");

                Type[] args = m.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) sb.Append(", ");

                    sb.Append("typename ");
                    sb.Append(args[i].Name);
                }

                sb.Append('>');
                sb.AppendLine();
            }

            if (!isGlobalFunc)
            {
                if (m.IsStatic) sb.Append("static ");
            }

            string rettype = string.Empty;

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
                    }
                }
            }

            sb.Append(rettype);
            sb.Append(' ');
            sb.Append(m.Name);

            sb.Append('(');

            for (int i = 0; i < pars.Length; i++)
            {
                if (i >= 1) sb.Append(", ");
                sb.Append(GetTypeString(pars[i].ParameterType));
                sb.Append(' ');

                string parname = pars[i].Name;

                if (string.IsNullOrEmpty(parname))
                {
                    parname = "par" + (i + 1).ToString();
                }

                sb.Append(parname);
            }

            sb.Append(')');

            //due to K&R braces in C++ the opening brace is effectively a part of signature
            sb.Append(" {");

            return sb.ToString();
        }
    }
}
