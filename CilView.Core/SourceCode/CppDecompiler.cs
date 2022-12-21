/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.Syntax;
using CilView.Common;

namespace CilView.SourceCode
{
    class CppDecompiler : Decompiler
    {
        public CppDecompiler(MethodBase method) : base(method)
        {

        }

        static void GetTypeTokens(Type t, List<SourceToken> target)
        {
            if (t == null) return;

            if (t.IsArray && t.GetArrayRank() == 1)
            {
                target.Add(new SourceToken("array", TokenKind.Keyword, "", " "));
                target.Add(new SourceToken("<", TokenKind.Punctuation));
                GetTypeTokens(t.GetElementType(), target);
                target.Add(new SourceToken(">", TokenKind.Punctuation, "", " "));
                target.Add(new SourceToken("^", TokenKind.Punctuation));
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

                if (t.IsClass || t.IsInterface)
                {
                    target.Add(new SourceToken("^", TokenKind.Punctuation, " ", ""));
                    return;
                }

                return;
            }

            //reference types are represented by handles in C++/CLI
            if (t.IsClass || t.IsInterface)
            {
                target.Add(new SourceToken(t.Name, TokenKind.TypeName, "", " "));
                target.Add(new SourceToken("^", TokenKind.Punctuation));
                return;
            }

            //process built-in types
            SourceToken tok = ProcessCommonTypes(t);

            if (tok != null)
            {
                target.Add(tok);
                return;
            }

            if (Utils.TypeEquals(t, typeof(uint)))
            {
                target.Add(new SourceToken("unsigned int", TokenKind.Keyword));
            }
            else if (Utils.TypeEquals(t, typeof(ushort)))
            {
                target.Add(new SourceToken("unsigned short", TokenKind.Keyword));
            }
            else if (Utils.TypeEquals(t, typeof(byte)))
            {
                target.Add(new SourceToken("unsigned char", TokenKind.Keyword));
            }
            else if (Utils.TypeEquals(t, typeof(sbyte)))
            {
                target.Add(new SourceToken("signed char", TokenKind.Keyword));
            }
            else if (Utils.TypeEquals(t, typeof(char)))
            {
                target.Add(new SourceToken("wchar_t", TokenKind.Keyword));
            }
            else
            {
                target.Add(new SourceToken(t.Name, TokenKind.TypeName));
            }
        }
        
        public override IEnumerable<SourceToken> GetMethodSigTokens()
        {
            MethodBase m = this._method;
            List<SourceToken> ret = new List<SourceToken>();
            
            ParameterInfo[] pars = m.GetParameters();

            //global functions don't have access modifiers
            bool isGlobalFunc = m.DeclaringType == null || Utils.StringEquals(m.DeclaringType.Name, "<Module>");

            if (!isGlobalFunc)
            {
                if (m.IsPublic)
                {
                    ret.Add(new SourceToken("public", TokenKind.Keyword));
                    ret.Add(new SourceToken(":", TokenKind.Punctuation, "", " "));
                }
                else if (m.IsFamily)
                {
                    ret.Add(new SourceToken("protected", TokenKind.Keyword));
                    ret.Add(new SourceToken(":", TokenKind.Punctuation, "", " "));
                }
                else if (m.IsAssembly)
                {
                    ret.Add(new SourceToken("internal", TokenKind.Keyword));
                    ret.Add(new SourceToken(":", TokenKind.Punctuation, "", " "));
                }
            }

            if (m.IsGenericMethod)
            {
                ret.Add(new SourceToken("generic", TokenKind.Keyword, "", " "));
                ret.Add(new SourceToken("<", TokenKind.Punctuation));
                Type[] args = m.GetGenericArguments();

                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) ret.Add(new SourceToken(",", TokenKind.Punctuation, "", " "));

                    ret.Add(new SourceToken("typename", TokenKind.Keyword, "", " "));
                    ret.Add(new SourceToken(args[i].Name, TokenKind.Name));
                }

                ret.Add(new SourceToken(">", TokenKind.Punctuation, "", Environment.NewLine));
            }

            if (!isGlobalFunc)
            {
                if (m.IsStatic) ret.Add(new SourceToken("static", TokenKind.Keyword, "", " "));
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
            ret.Add(new SourceToken("(", TokenKind.Punctuation));
            
            for (int i = 0; i < pars.Length; i++)
            {
                if (i >= 1) ret.Add(new SourceToken(",", TokenKind.Punctuation, "", " "));
                GetTypeTokens(pars[i].ParameterType, ret);
                ret.Add(new SourceToken(" ", TokenKind.Unknown));
                
                string parname = pars[i].Name;

                if (string.IsNullOrEmpty(parname))
                {
                    parname = "par" + (i + 1).ToString();
                }

                ret.Add(new SourceToken(parname, TokenKind.Name));
            }

            ret.Add(new SourceToken(")", TokenKind.Punctuation, "", ""));

            //due to K&R braces in C++ the opening brace is effectively a part of signature
            ret.Add(new SourceToken("{", TokenKind.Punctuation));

            return ret;
        }
    }
}
