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

        public override string GetMethodSigString()
        {
            MethodBase m = this._method;
            StringBuilder sb = new StringBuilder(500);
            ParameterInfo[] pars = m.GetParameters();

            if (m.IsPublic) sb.Append("public ");
            else if (m.IsFamily) sb.Append("protected ");
            else if (m.IsAssembly) sb.Append("internal ");

            if (m.IsStatic) sb.Append("static ");

            string rettype = string.Empty;

            if (m is CustomMethod)
            {
                CustomMethod cm = (CustomMethod)m;
                Type t = cm.ReturnType;

                if (t != null)
                {
                    if (Utils.StringEquals(t.FullName, "System.Void"))
                    {
                        rettype = "void";
                    }
                    else
                    {
                        rettype = t.Name;
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
                sb.Append(pars[i].ParameterType.Name);
                sb.Append(' ');

                string parname = pars[i].Name;

                if (string.IsNullOrEmpty(parname))
                {
                    parname = "par" + (i + 1).ToString();
                }

                sb.Append(parname);
            }

            sb.Append(')');
            return sb.ToString();
        }
    }
}
