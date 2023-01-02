/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CilTools.Syntax.Tokens
{
    /// <summary>
    /// Reads raw tokens from a string using the specified collection of token definitions
    /// </summary>
    public class TokenReader
    {
        char[] _source;
        int _pos = 0;
        SyntaxTokenDefinition[] tokens;

        /// <summary>
        /// Creates a new instance of the token reader
        /// </summary>
        /// <param name="src">Source string</param>
        /// <param name="tokenDefinitions">Collection of token definitions to use</param>
        public TokenReader(string src, IEnumerable<SyntaxTokenDefinition> tokenDefinitions)
        {
            this._source = src.ToCharArray();
            this.tokens = tokenDefinitions.ToArray();
        }

        string ReadToken()
        {
            if (_pos >= _source.Length) return string.Empty;

            StringBuilder sb = new StringBuilder();
            SyntaxTokenDefinition currentToken = null;
            
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

        /// <summary>
        /// Reads all tokens from the current instance
        /// </summary>
        /// <returns>Collection of strings that contain tokens</returns>
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

        /// <summary>
        /// Gets a next character in this token reader without advancing a current position
        /// </summary>
        /// <returns>Next character in the current position, or zero if the end of string is reached.</returns>
        public char PeekChar()
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
