/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.SourceCode
{
    public class VbClassifier : TokenClassifier
    {
        //https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/keywords/
        static readonly HashSet<string> keywords = new HashSet<string>(new string[] {
"AddHandler","AddressOf","Alias","And","AndAlso","As","Boolean","ByRef","Byte","ByVal","Call",
"Case","Catch","CBool","CByte","CChar","CDate","CDbl","CDec","Char","CInt","Class","CLng","CObj",
"Const","Continue","CSByte","CShort","CSng","CStr","CType","CUInt","CULng","CUShort","Date",
"Decimal","Declare","Default","Delegate","Dim","DirectCast","Do","Double","Each","Else","ElseIf",
"End","EndIf","Enum","Erase","Error","Event","Exit","False","Finally","For","Friend","Function",
"Get","GetType","GetXMLNamespace","Global","GoSub","GoTo","Handles","If","Implements","Imports",
"In","Inherits","Integer","Interface","Is","IsNot","Let","Lib","Like","Long","Loop","Me",
"Mod","Module","MustInherit","MustOverride","MyBase","MyClass","NameOf","Namespace","Narrowing",
"New","Next","Not","Nothing","NotInheritable","NotOverridable","Object","Of","On","Operator",
"Option","Optional","Or","OrElse","Out","Overloads","Overridable","Overrides","ParamArray",
"Partial","Private","Property","Protected","Public","RaiseEvent","ReadOnly","ReDim","REM",
"RemoveHandler","Resume","Return","SByte","Select","Set","Shadows","Shared","Short","Single",
"Static","Step","Stop","String","Structure","Sub","SyncLock","Then","Throw","To","True","Try",
"TryCast","TypeOf","UInteger","ULong","UShort","Using","Variant","Wend","When","While","Widening",
"With","WithEvents","WriteOnly","Xor",
"#Const","#Else","#ElseIf","#End","#If"
        }, StringComparer.OrdinalIgnoreCase);

        static bool IsKeyword(string token)
        {
            return keywords.Contains(token);
        }

        static SourceTokenKind GetKindImpl(string token)
        {
            if (token.Length == 0) return SourceTokenKind.Unknown;

            if (char.IsDigit(token[0]))
            {
                return SourceTokenKind.NumericLiteral;
            }
            else if (token[0] == '_')
            {
                return SourceTokenKind.OtherName;
            }
            else if (token[0] == '"')
            {
                if (token.Length < 2 || token[token.Length - 1] != '"')
                {
                    return SourceTokenKind.Unknown;
                }
                else
                {
                    return SourceTokenKind.StringLiteral;
                }
            }
            else if (token[0] == '\'')
            {
                return SourceTokenKind.Comment;
            }
            else if (char.IsPunctuation(token[0]) || char.IsSymbol(token[0]))
            {
                return SourceTokenKind.Punctuation;
            }
            else
            {
                return SourceTokenKind.Unknown;
            }
        }

        public override SourceTokenKind GetKind(string token)
        {
            if (token.Length == 0) return SourceTokenKind.Unknown;

            if (char.IsLetter(token[0]) || token[0] == '#')
            {
                if (IsKeyword(token)) return SourceTokenKind.Keyword;
                else return SourceTokenKind.OtherName;
            }
            else return GetKindImpl(token);
        }
    }
}
