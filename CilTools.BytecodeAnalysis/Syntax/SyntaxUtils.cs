/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;

namespace CilTools.Syntax
{
    static class SyntaxUtils
    {
        // Chars valid in DottedName token (ECMA-335 II.5.2 - Basic syntax categories)
        static readonly HashSet<char> validIdChars = new HashSet<char>(new char[] { '.', '_', '$', '@', '`', '?' });

        internal static string GetIndentString(int c)
        {
            return "".PadLeft(c, ' ');
        }

        internal static bool IsValidIdStartingCharacter(char c)
        {
            return char.IsLetter(c) || validIdChars.Contains(c);
        }

        internal static bool IsValidIdCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || validIdChars.Contains(c);
        }

        internal static string EscapeIdentifier(string id)
        {
            if (string.IsNullOrEmpty(id)) return string.Empty;

            if (id[0] == '\'') return id; //already escaped

            //ECMA-335 VI.C.1 - ILAsm keywords
            if (SyntaxClassifier.IsKeyword(id)) return "'" + id + "'";

            //ECMA 335 II.5 - General syntax
            if (!IsValidIdStartingCharacter(id[0])) return "'" + id + "'";

            for (int i = 1; i < id.Length; i++)
            {
                if (!IsValidIdCharacter(id[i])) return "'" + id + "'";
            }

            return id; //no need to escape
        }
    }
}
