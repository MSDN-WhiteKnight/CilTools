/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using CilTools.Reflection;
using CilView.Common;

namespace CilView.SourceCode
{
    class CsharpDecompiler : Decompiler
    {
        public CsharpDecompiler(MethodBase method) : base(method)
        {

        }

        static string GetTypeString(Type t)
        {
            if (t == null) return string.Empty;

            if (t.IsArray && t.GetArrayRank() == 1)
            {
                return GetTypeString(t.GetElementType()) + "[]";
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

                sb.Append('>');
                return sb.ToString();
            }

            //process built-in types
            string s = ProcessCommonTypes(t);

            if (s != null) return s;

            if (Utils.TypeEquals(t, typeof(string)))      return "string";
            else if (Utils.TypeEquals(t, typeof(uint)))   return "uint";
            else if (Utils.TypeEquals(t, typeof(ushort))) return "ushort";
            else if (Utils.TypeEquals(t, typeof(long)))   return "long";
            else if (Utils.TypeEquals(t, typeof(ulong)))  return "ulong";
            else if (Utils.TypeEquals(t, typeof(byte)))   return "byte";
            else if (Utils.TypeEquals(t, typeof(sbyte)))  return "sbyte";
            else if (Utils.TypeEquals(t, typeof(char)))   return "char";
            else if (Utils.TypeEquals(t, typeof(object))) return "object";

            return t.Name;
        }

        static void PrintModififers(TextWriter target, MethodBase m)
        {
            if (m.IsPublic) target.Write("public ");
            else if (m.IsFamily) target.Write("protected ");
            else if (m.IsAssembly) target.Write("internal ");

            if (m.IsStatic) target.Write("static ");
            if (m.IsAbstract) target.Write("abstract ");

            target.Flush();
        }

        static string DecompilePropertyMethod(MethodBase m)
        {
            string mName = m.Name;

            if (string.IsNullOrEmpty(mName)) return string.Empty;
            if (!mName.Contains("_")) return string.Empty;

            string[] arr = mName.Split('_');
            if(arr.Length<2) return string.Empty;

            string accName = arr[0];
            string propName = arr[1];

            StringBuilder sb = new StringBuilder(200);
            StringWriter wr = new StringWriter(sb);
            PrintModififers(wr, m);

            string proptype = string.Empty;

            if (m is ICustomMethod)
            {
                ICustomMethod cm = (ICustomMethod)m;
                Type t = cm.ReturnType;

                if (t != null)
                {
                    proptype = GetTypeString(t);
                }
            }

            wr.Write(proptype);
            wr.Write(' ');
            wr.Write(propName);
            wr.Write(' ');
            wr.Write('{');
            wr.Write(accName);
            wr.Write(';');
            wr.Write('}');
            return sb.ToString();
        }

        public override string GetMethodSigString()
        {
            MethodBase m = this._method;

            if (Utils.IsPropertyMethod(m))
            {
                string propmethod = DecompilePropertyMethod(m);
                if (propmethod.Length > 0) return propmethod;
            }

            StringBuilder sb = new StringBuilder(500);
            ParameterInfo[] pars = m.GetParameters();
            StringWriter wr = new StringWriter(sb);
            
            if (!Utils.IsAbstractInterfaceMethod(m))
            {
                PrintModififers(wr, m);
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

            if (m.IsGenericMethod)
            {
                sb.Append('<');

                Type[] args = m.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) sb.Append(", ");

                    sb.Append(args[i].Name);
                }

                sb.Append('>');
            }

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

            if (m.IsAbstract) sb.Append(';');

            return sb.ToString();
        }
    }
}
