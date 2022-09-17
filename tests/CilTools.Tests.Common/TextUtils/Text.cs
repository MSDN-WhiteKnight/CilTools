/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace CilTools.Tests.Common.TextUtils
{
    /// <summary>
    /// Represents an element of the strings sequence. The element could be either a literal 
    /// string or a mask allowing multiple string values.
    /// </summary>
    /// <remarks>
    /// This class is used in tests to verify that CIL code produced by disassembler 
    /// matches the expected pattern. It is a high-level wrapper for a regular expressions
    /// mechanism.
    /// </remarks>
    public abstract class Text
    {
        /// <summary>
        /// Converts this element to the equivalent regex representation
        /// </summary>
        public abstract string GetString();

        /// <summary>
        /// Gets unescaped string that matches this text element
        /// </summary>
        public abstract string GetRawString();

        public override string ToString()
        {
            return GetString();
        }

        /// <summary>
        /// Defines a text element allowing any string values
        /// </summary>
        public static Text Any
        {
            get { return AnyCharsText.Value; }
        }

        /// <summary>
        /// Defines a text element allowing one or more whitespace characters
        /// </summary>
        public static Text Whitespace
        {
            get { return AtLeastOneWhitespaceText.Value; }
        }

        /// <summary>
        /// Checks whether a specified string matches a specified sequence of text elements
        /// </summary>
        public static bool IsMatch(string s, Text[] match)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < match.Length; i++)
            {
                sb.Append(match[i].GetString());
            }

            string pattern = sb.ToString();
            return Regex.IsMatch(s, pattern);
        }

        /// <summary>
        /// Gets the minimal string that matches the specified sequence of text elements
        /// </summary>
        public static string GetMinMatchingText(Text[] items)
        {
            StringBuilder sb = new StringBuilder(1000);

            for (int i = 0; i < items.Length; i++)
            {
                sb.Append(items[i].GetRawString());
            }

            return sb.ToString();
        }

        public static implicit operator Text(string s) => new Literal(s);
    }

    /// <summary>
    /// Represents a text element with the exact string value
    /// </summary>
    public class Literal : Text
    {
        string val;
        string raw;

        public Literal(string v)
        {
            this.raw = v;
            this.val = Regex.Escape(v);
        }

        public override string GetString()
        {
            return this.val;
        }

        public override string GetRawString()
        {
            return this.raw;
        }
    }

    /// <summary>
    /// Represents a text element allowing any sequence of characters
    /// </summary>
    public class AnyCharsText : Text
    {
        static AnyCharsText val = null;

        protected AnyCharsText() { }

        /// <summary>
        /// Provides the singleton value for the AnyCharsText class
        /// </summary>
        public static AnyCharsText Value
        {
            get
            {
                if (val == null) val = new AnyCharsText();
                return val;
            }
        }

        public override string GetString()
        {
            return "[\\s\\S]*";
        }

        public override string GetRawString()
        {
            return " ";
        }
    }

    public class AtLeastOneWhitespaceText : Text
    {
        static AtLeastOneWhitespaceText val = null;

        AtLeastOneWhitespaceText() { }

        /// <summary>
        /// Provides the singleton value for the AtLeastOneWhitespaceText class
        /// </summary>
        public static AtLeastOneWhitespaceText Value
        {
            get
            {
                if (val == null) val = new AtLeastOneWhitespaceText();
                return val;
            }
        }

        public override string GetString()
        {
            return "\\s+";
        }

        public override string GetRawString()
        {
            return " ";
        }
    }
}
