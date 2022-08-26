/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using CilView.Common;
using CilView.Core.Syntax;

namespace CilView.SourceCode
{
    class CsharpDecompiler : Decompiler
    {
        public CsharpDecompiler(MethodBase method) : base(method)
        {

        }

        static void GetTypeTokens(Type t, List<SourceToken> target)
        {
            if (t == null) return;

            if (t.IsArray && t.GetArrayRank() == 1)
            {
                GetTypeTokens(t.GetElementType(), target);
                target.Add(new SourceToken("[]", TokenKind.Punctuation));
                return;
            }

            if (t.IsByRef)
            {
                target.Add(new SourceToken("ref", TokenKind.Keyword, "", " "));
                GetTypeTokens(t.GetElementType(), target);
                return;
            }

            if (t.IsPointer)
            {
                Type elementType = t.GetElementType();

                if (Utils.StringEquals(elementType.FullName, "System.Void"))
                {
                    target.Add(new SourceToken("void", TokenKind.Keyword));
                }
                else
                {
                    GetTypeTokens(elementType, target);
                }

                target.Add(new SourceToken("*", TokenKind.Punctuation));
                return;
            }

            if (t.IsGenericParameter)
            {
                target.Add(new SourceToken(t.Name, TokenKind.Name));
                return;
            }

            if (t.IsGenericType)
            {
                target.Add(new SourceToken(GetGenericDefinitionName(t.Name), TokenKind.TypeName));
                target.Add(new SourceToken("<", TokenKind.Punctuation));
                Type[] args = t.GetGenericArguments();

                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) target.Add(new SourceToken(",", TokenKind.Punctuation, "", " "));

                    GetTypeTokens(args[i], target);
                }

                target.Add(new SourceToken(">", TokenKind.Punctuation));
                return;
            }

            //process built-in types
            SourceToken tok = ProcessCommonTypes(t);

            if (tok != null)
            {
                target.Add(tok);
                return;
            }

            if (Utils.TypeEquals(t, typeof(string)))      target.Add(new SourceToken("string", TokenKind.Keyword));
            else if (Utils.TypeEquals(t, typeof(uint)))   target.Add(new SourceToken("uint", TokenKind.Keyword));
            else if (Utils.TypeEquals(t, typeof(ushort))) target.Add(new SourceToken("ushort", TokenKind.Keyword));
            else if (Utils.TypeEquals(t, typeof(long)))   target.Add(new SourceToken("long", TokenKind.Keyword));
            else if (Utils.TypeEquals(t, typeof(ulong)))  target.Add(new SourceToken("ulong", TokenKind.Keyword));
            else if (Utils.TypeEquals(t, typeof(byte)))   target.Add(new SourceToken("byte", TokenKind.Keyword));
            else if (Utils.TypeEquals(t, typeof(sbyte)))  target.Add(new SourceToken("sbyte", TokenKind.Keyword));
            else if (Utils.TypeEquals(t, typeof(char)))   target.Add(new SourceToken("char", TokenKind.Keyword));
            else if (Utils.TypeEquals(t, typeof(object))) target.Add(new SourceToken("object", TokenKind.Keyword));
            else target.Add(new SourceToken(t.Name, TokenKind.TypeName));
        }
        
        static void GetModifiers(MethodBase m, List<SourceToken> target)
        {
            if (m.IsPublic) target.Add(new SourceToken("public", TokenKind.Keyword, "", " "));
            else if (m.IsFamily) target.Add(new SourceToken("protected", TokenKind.Keyword, "", " "));
            else if (m.IsAssembly) target.Add(new SourceToken("internal", TokenKind.Keyword, "", " "));

            if (m.IsStatic) target.Add(new SourceToken("static", TokenKind.Keyword, "", " "));
            if (m.IsAbstract) target.Add(new SourceToken("abstract", TokenKind.Keyword, "", " "));
        }
        
        static void DecompilePropertyMethod(MethodBase m, List<SourceToken> target)
        {
            string mName = m.Name;

            if (string.IsNullOrEmpty(mName)) return;
            if (!mName.Contains("_")) return;

            string[] arr = mName.Split('_');
            if (arr.Length < 2) return;

            string accName = arr[0];
            string propName = arr[1];
            
            GetModifiers(m, target);
            
            Type t = GetReturnType(m);

            if (t != null)
            {
                GetTypeTokens(t, target);
                target.Add(new SourceToken(" ", TokenKind.Unknown));
            }
            
            target.Add(new SourceToken(propName, TokenKind.Name, "", " "));
            target.Add(new SourceToken("{", TokenKind.Punctuation, "", ""));
            target.Add(new SourceToken(accName, TokenKind.Keyword, "", ""));
            target.Add(new SourceToken(";", TokenKind.Punctuation, "", ""));
            target.Add(new SourceToken("}", TokenKind.Punctuation, "", ""));
        }

        static string EscapeString(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;

            StringBuilder sb = new StringBuilder(str.Length * 2);

            foreach (char c in str)
            {
                switch (c)
                {
                    case '\0': sb.Append("\\0"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\a': sb.Append("\\a"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\"); break;

                    default:
                        if (char.IsControl(c)) sb.Append("\\u" + ((ushort)c).ToString("X").PadLeft(4, '0'));
                        else sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        static void GetConstantValueTokens(object constant, List<SourceToken> target)
        {
            if (constant != null)
            {
                if (Utils.StringEquals(constant.GetType().FullName, "System.String"))
                {
                    target.Add(new SourceToken("\"" + EscapeString(constant.ToString()) + "\"",
                        TokenKind.DoubleQuotLiteral));
                }
                else if (Utils.StringEquals(constant.GetType().FullName, "System.Char"))
                {
                    target.Add(new SourceToken("'" + constant.ToString() + "'",
                        TokenKind.SingleQuotLiteral));
                }
                else if (Utils.StringEquals(constant.GetType().FullName, "System.Boolean"))
                {
                    target.Add(new SourceToken(constant.ToString().ToLower(), TokenKind.Keyword));
                }
                else //most of the types...
                {
                    target.Add(new SourceToken(Convert.ToString(constant, CultureInfo.InvariantCulture),
                        TokenKind.NumericLiteral));
                }
            }
            else
            {
                target.Add(new SourceToken("null", TokenKind.Keyword));
            }
        }

        public override IEnumerable<SourceToken> GetMethodSigTokens()
        {
            MethodBase m = this._method;

            if (Utils.IsPropertyMethod(m))
            {
                List<SourceToken> propmethod = new List<SourceToken>();
                DecompilePropertyMethod(m, propmethod);
                if (propmethod.Count > 0) return propmethod;
            }
            
            ParameterInfo[] pars = m.GetParameters();
            List<SourceToken> ret = new List<SourceToken>();

            if (!Utils.IsAbstractInterfaceMethod(m))
            {
                GetModifiers(m, ret);
            }

            Type t = GetReturnType(m);

            if (t != null)
            {
                if (Utils.StringEquals(t.FullName, "System.Void"))
                {
                    ret.Add(new SourceToken("void", TokenKind.Keyword, "", " "));
                }
                else
                {
                    GetTypeTokens(t, ret);
                    ret.Add(new SourceToken(" ", TokenKind.Unknown));
                }
            }

            ret.Add(new SourceToken(m.Name, TokenKind.FunctionName));
            
            if (m.IsGenericMethod)
            {
                ret.Add(new SourceToken("<", TokenKind.Punctuation));
                Type[] args = m.GetGenericArguments();

                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) ret.Add(new SourceToken(",", TokenKind.Punctuation, "", " "));

                    ret.Add(new SourceToken(args[i].Name, TokenKind.Name));
                }

                ret.Add(new SourceToken(">", TokenKind.Punctuation));
            }

            ret.Add(new SourceToken("(", TokenKind.Punctuation));
            
            for (int i = 0; i < pars.Length; i++)
            {
                if (i >= 1)
                {
                    ret.Add(new SourceToken(",", TokenKind.Punctuation, "", " "));
                }

                GetTypeTokens(pars[i].ParameterType, ret);
                ret.Add(new SourceToken(" ", TokenKind.Unknown));

                string parname = pars[i].Name;

                if (string.IsNullOrEmpty(parname))
                {
                    parname = "par" + (i + 1).ToString();
                }

                ret.Add(new SourceToken(parname, TokenKind.Name));

                //default value for optional parameters
                if (pars[i].IsOptional && pars[i].RawDefaultValue != DBNull.Value)
                {
                    ret.Add(new SourceToken("=", TokenKind.Punctuation, " ", " "));
                    GetConstantValueTokens(pars[i].RawDefaultValue, ret);
                }
            }

            ret.Add(new SourceToken(")", TokenKind.Punctuation));

            if (m.IsAbstract) ret.Add(new SourceToken(";", TokenKind.Punctuation));

            return ret;
        }
    }
}
