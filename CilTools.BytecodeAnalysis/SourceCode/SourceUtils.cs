/* CilTools.BytecodeAnalysis library 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.SourceCode
{
    internal static class SourceUtils
    {
        public static string[] SplitSourceCodeFragment(string s) 
        {
            //ignore strings that consist of a single significant character
            if (s.Length <= 1 || (s.Length<=3 && s.EndsWith("\r\n"))) return new string[0];

            //prepare source code string to be inserted into disassembly as comments
            string normalized = s.Replace("\r\n", "\n");
            string[] arr = normalized.Split('\n');

            //don't bother doing anything if there's only a single line
            if (arr.Length <= 1) return new string[] { s };

            int len = arr.Length;

            //trim trailing empty lines
            for (int i = arr.Length - 1; i > 0; i--) 
            {
                if (arr[i].Length == 0) len--;
                else break;
            }

            if (len <= 0) len = 1;

            string[] arr_ret=new string[len];

            //deindent lines
            for (int i = 0; i < len; i++) 
            {
                arr_ret[i] = " "+arr[i].Trim();
            }

            arr_ret[len - 1] += Environment.NewLine;

            return arr_ret;
        }
    }
}
