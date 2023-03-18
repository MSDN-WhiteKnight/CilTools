/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CilTools.Syntax;

namespace CilTools.Tests.Common
{
    public static class Utils
    {
        /// <summary>
        /// Provides <see cref="BindingFlags"/> mask that matches all members 
        /// (public and non-public, static and instance)
        /// </summary>
        public static BindingFlags AllMembers()
        {
            return BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance;
        }

        public static string SyntaxToString(IEnumerable<SyntaxNode> nodes) 
        {
            StringBuilder sb = new StringBuilder();
            StringWriter wr = new StringWriter(sb);

            foreach (SyntaxNode node in nodes)
            {
                node.ToText(wr);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets current project configuration name (Debug or Release)
        /// </summary>
        public static string GetConfig() 
        {
#if DEBUG 
            return "Debug";
#else
            return "Release";
#endif
        }

        public static bool IsDebugBuild()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        public static string GetSampleAppPath()
        {
            AssemblyName an = typeof(object).Assembly.GetName();
            string fmt;

            if (an.Name.Equals("System.Private.CoreLib", StringComparison.Ordinal))
            {
                //.NET Core
                fmt = @"..\..\..\..\EmitSampleApp\bin\{0}\netcoreapp3.1\win-x86\EmitSampleApp.dll";
            }
            else
            {
                //.NET Framework
                fmt = @"..\..\..\..\EmitSampleApp\bin\{0}\net45\win-x86\EmitSampleApp.exe";
            }

            return string.Format(fmt, GetConfig());
        }

        public static void GenerateFakeIL(int repeats, TextWriter target)
        {
            string[] words = {
                "Test","Foo","Bar","Buzz","Frobby","Bobby","Hello","Alice","Bob","Lee","Miroslav","Nicolas","Peter"
            };

            Random rnd = new Random();

            for (int i = 0; i < repeats; i++)
            {
                int nWord = rnd.Next(words.Length);
                int nNumber = rnd.Next();

                string name = words[nWord] + nNumber.ToString();
                string str = ".method public static void " + name + "() cil managed { } ";
                target.Write(str);

                nWord = rnd.Next(words.Length);
                nNumber = rnd.Next();
                str = "//" + words[nWord] + nNumber.ToString();
                target.WriteLine(str);
            }

            target.Flush();
        }

        public static string GenerateFakeIL(int repeats)
        {
            StringBuilder sb = new StringBuilder(5000);
            StringWriter wr = new StringWriter(sb);
            GenerateFakeIL(repeats, wr);
            return sb.ToString();
        }

        public static string GetStringCapped(string input, int maxCount)
        {
            if (input.Length < maxCount) return input;
            else return input.Substring(0, maxCount - 4) + "...";
        }

        public static string GetProgramDir()
        {
            return Path.GetDirectoryName(typeof(Utils).Assembly.Location);
        }

        static readonly Random rng = new Random();

        public static string GetRandomFilePath(string start, int n, string ext)
        {
            StringBuilder sb = new StringBuilder(n);
            sb.Append(start);
            sb.Append(DateTime.Now.ToString("yyyyMMdd_hhmmss"));
            sb.Append('_');
            int nLetters = 'Z' - 'A';
            int nDigits = 10;

            for (int i = 0; i < n; i++)
            {
                int x = rng.Next(nLetters + nDigits);

                if (x <= 9)
                {
                    sb.Append(x.ToString());
                }
                else
                {
                    sb.Append((char)('A' + (x - 9)));
                }
            }
            
            sb.Append('.');
            sb.Append(ext);
            return Path.Combine(GetProgramDir(), sb.ToString());
        }

        /// <summary>
        /// Gets assembly version in IL format (A:B:C:D)
        /// </summary>
        public static string GetVersionIL(Assembly ass)
        {
            return ass.GetName().Version.ToString(4).Replace(".", ":");
        }

        /// <summary>
        /// Normalize string to replace all whitespace sequences with a single whitespace
        /// </summary>
        public static string NormalizeWhitespace(string input)
        {
            char[] splitter = new char[] { ' ', '\t', '\r', '\n' };
            string[] arr = input.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder(input.Length);

            for (int i = 0; i < arr.Length; i++)
            {
                sb.Append(arr[i]);

                if (i < arr.Length - 1) sb.Append(' ');
            }

            return sb.ToString();
        }

        public static string MemberToString(MemberInfo member)
        {
            StringBuilder sb = new StringBuilder();

            if (member is MethodBase)
            {
                MethodBase mb = (MethodBase)member;
                sb.Append(mb.Name);
                sb.Append('(');

                ParameterInfo[] pars = mb.GetParameters();

                for (int i = 0; i < pars.Length; i++)
                {
                    if (i >= 1) sb.Append(',');
                    sb.Append(pars[i].ParameterType.Name);
                }

                sb.Append(')');
            }
            else
            {
                sb.Append(member.Name);
            }

            return sb.ToString();
        }
    }
}
