/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.Syntax;

namespace CilView.SourceCode
{
    public class CsharpClassifier : TokenClassifier
    {
        //https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/
        static readonly HashSet<string> keywords = new HashSet<string>(new string[] {
"abstract","as","base","bool","break","byte","case","catch","char",
"checked","class","const","continue","decimal","default","delegate","do",
"double","else","enum","event","explicit","extern","false","finally","fixed",
"float","for","foreach","goto","if","implicit","in","int","interface","internal","is",
"lock","long","namespace","new","null","object","operator","out","override","params",
"private","protected","public","readonly","ref","return","sbyte","sealed","short",
"sizeof","stackalloc","static","string","struct","switch","this","throw","true",
"try","typeof","uint","ulong","unchecked","unsafe","ushort","using","virtual","void",
"volatile","while"
        });

        static bool IsKeyword(string token)
        {
            return keywords.Contains(token);
        }

        public override TokenKind GetKind(string token)
        {
            if (token.Length == 0) return TokenKind.Unknown;

            if (char.IsLetter(token[0]) || token[0] == '_')
            {
                if (IsKeyword(token)) return TokenKind.Keyword;
                else return TokenKind.Name;
            }
            else return GetKindCommon(token);
        }
    }
}
