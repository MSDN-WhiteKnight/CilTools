/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.SourceCode
{
    public class CppClassifier : TokenClassifier
    {
        //https://docs.microsoft.com/en-us/cpp/cpp/keywords-cpp?view=msvc-170
        static readonly HashSet<string> keywords = new HashSet<string>(new string[] {
"alignas","alignof","and","and_eq","asm","auto","bitand","bitor","bool","break",
"case","catch","char","char8_t","char16_t","char32_t","class","compl","concept",
"const","const_cast","consteval","constexpr","constinit","continue","co_await",
"co_return","co_yield","decltype","default","delete","do","double","dynamic_cast",
"else","enum","explicit","export","extern","false","float","for","friend",
"goto","if","inline","int","long","mutable","namespace","new","noexcept",
"not","not_eq","nullptr","operator","or","or_eq","private","protected",
"public","register","reinterpret_cast","requires","return","short","signed",
"sizeof","static","static_assert","static_cast","struct","switch","template",
"this","thread_local","throw","true","try","typedef","typeid","typename",
"union","unsigned","using","virtual","void","volatile","wchar_t","while",
"xor","xor_eq",

"abstract","array","delegate","event","finally","gcnew","generic","initonly",
"literal","property","sealed"
        });

        static bool IsKeyword(string token)
        {
            return keywords.Contains(token);
        }

        public override SourceTokenKind GetKind(string token)
        {
            if (token.Length == 0) return SourceTokenKind.Unknown;

            if (char.IsLetter(token[0]) || token[0] == '_')
            {
                if (IsKeyword(token)) return SourceTokenKind.Keyword;
                else return SourceTokenKind.OtherName;
            }
            else return GetKindCommon(token);
        }
    }
}
