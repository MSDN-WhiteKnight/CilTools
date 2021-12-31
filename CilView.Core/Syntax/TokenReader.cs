/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.Core.Syntax
{
    public class TokenReader
    {
        char[] _source;
        int _pos = 0;

        public TokenReader(string src)
        {
            this._source = src.ToCharArray();
        }

        public string ReadToken()
        {
            if (_pos >= _source.Length) return string.Empty;

            StringBuilder sb = new StringBuilder();
            SyntaxToken currentToken = null;
            SyntaxToken[] tokens = SyntaxToken.AllTokens;

            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i].HasStart(this))
                {
                    currentToken = tokens[i];
                    break;
                }
            }

            if (currentToken == null)
            {
                //unknown token
                System.Diagnostics.Debug.Assert(false, "Unknown token at " + this._pos.ToString());

                while (true)
                {
                    char c = this.ReadChar();

                    if (c == (char)0) break;

                    sb.Append(c);

                    for (int i = 0; i < tokens.Length; i++)
                    {
                        if (tokens[i].HasStart(this)) return sb.ToString();
                    }
                }
            }
            else
            {
                while (true)
                {
                    char c = this.ReadChar();

                    if (c == (char)0) break;

                    sb.Append(c);

                    if (!currentToken.HasContinuation(sb.ToString(), this))
                    {
                        break;
                    }
                }
            }

            return sb.ToString();
        }

        public IEnumerable<string> ReadAll() 
        {
            while (true)
            {
                string s = this.ReadToken();
                if (string.IsNullOrEmpty(s)) break;
                yield return s;
            }
        }

        internal char ReadChar()
        {
            if (_pos >= _source.Length) return (char)0;

            char ret = _source[_pos];
            _pos++;
            return ret;
        }

        internal char PeekChar()
        {
            if (_pos >= _source.Length) return (char)0;
            else return _source[_pos];
        }

        internal char GetPreviousChar(int offset)
        {
            if (_pos - offset < 0) return (char)0;
            else return _source[_pos - offset];
        }

        internal char[] PeekChars(int n)
        {
            if (_pos + n >= _source.Length) return new char[0];

            char[] ret = new char[n];

            for (int i = 0; i < n; i++)
            {
                ret[i] = _source[_pos + i];
            }

            return ret;
        }
    }
}
