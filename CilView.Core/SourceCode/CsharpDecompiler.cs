/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
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
                target.Add(new SourceToken(sb.ToString(), TokenKind.Unknown));
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
            SourceToken tok = ProcessCommonTypes(t);

            if (tok != null) return tok.Content;

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
                StringBuilder sb = new StringBuilder(100);
                sb.Append('<');

                Type[] args = m.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) sb.Append(", ");

                    sb.Append(args[i].Name);
                }

                sb.Append('>');
                ret.Add(new SourceToken(sb.ToString(), TokenKind.Unknown));
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
            }

            ret.Add(new SourceToken(")", TokenKind.Punctuation));

            if (m.IsAbstract) ret.Add(new SourceToken(";", TokenKind.Punctuation));

            return ret;
        }
    }
}
