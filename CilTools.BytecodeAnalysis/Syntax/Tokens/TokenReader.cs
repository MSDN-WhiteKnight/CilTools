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

        /// <summary>
        /// Gets the position in the source string at which the next token would be read
        /// </summary>
        public int Position
        {
            get { return this._pos; }
        }

        /// <summary>
        /// Gets the number of characters in the source string
        /// </summary>
        public int Length
        {
            get { return this._source.Length; }
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
                System.Diagnostics.Debug.WriteLine("Unknown token at " + this._pos.ToString());

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

        /// <summary>
        /// Gets a character at the specified offset before the current position in this token reader. Offset 0 means current 
        /// position, offset 1 means character immediately before current position, etc. Does not change the current position.
        /// </summary>
        /// <param name="offset">The offset of the character to return</param>
        /// <returns>Previous character at the specified offset, or zero if it is outside of the source string bounds.</returns>
        public char GetPreviousChar(int offset)
        {
            if (_pos - offset < 0 || _pos - offset >= _source.Length) return (char)0;
            else return _source[_pos - offset];
        }

        internal char[] PeekChars(int n)
        {
            if (_pos + n > _source.Length) return new char[0];

            char[] ret = new char[n];

            for (int i = 0; i < n; i++)
            {
                ret[i] = _source[_pos + i];
            }

            return ret;
        }

        /// <summary>
        /// Gets a specified number of next characters in this token reader without advancing a current position
        /// </summary>
        /// <param name="n">Number of characters to peek</param>
        /// <returns>
        /// String containing <c>n</c> next characters, or an empty string when the end of string is reached.
        /// </returns>
        public string PeekString(int n)
        {
            if (_pos >= _source.Length) return string.Empty;

            if (_pos + n > _source.Length) n = _source.Length - _pos;

            StringBuilder sb = new StringBuilder(n);

            for (int i = 0; i < n; i++)
            {
                sb.Append(_source[_pos + i]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the full source string used by this reader
        /// </summary>
        public string GetSourceString()
        {
            return new string(this._source);
        }
    }
}
