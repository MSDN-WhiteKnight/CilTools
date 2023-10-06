/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
// Copied from: https://gitflic.ru/project/smallsoft/html-forms/blob?file=Html.Forms%2FHtmlBuilder.cs&branch=main

namespace CilView.Common
{
    /// <summary>
    /// Provides high-level helper methods that generate HTML markup and write it into the specified target
    /// </summary>
    internal class HtmlBuilder
    {
        TextWriter wr;

        /// <summary>
        /// Creates a new <c>HtmlBuilder</c> that writes HTML into the specified <see cref="TextWriter"/>
        /// </summary>
        /// <exception cref="ArgumentNullException">Target is null</exception>
        public HtmlBuilder(TextWriter target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            this.wr = target;
        }

        /// <summary>
        /// Creates a new <c>HtmlBuilder</c> that writes HTML into the specified <see cref="StringBuilder"/>
        /// </summary>
        /// <exception cref="ArgumentNullException">Target is null</exception>
        public HtmlBuilder(StringBuilder target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            this.wr = new StringWriter(target);
        }

        internal TextWriter Target
        {
            get { return this.wr; }
        }

        internal static readonly HtmlAttribute[] NoAttributes = new HtmlAttribute[0];

        public static HtmlAttribute[] OneAttribute(string name, string val)
        {
            return new HtmlAttribute[] { new HtmlAttribute(name, val) };
        }

        /// <summary>
        /// Writes the specified string unmodified
        /// </summary>
        public void WriteRaw(string s)
        {
            wr.Write(s);
        }

        /// <summary>
        /// Writes the specified string, escaping special HTML characters
        /// </summary>
        public void WriteEscaped(string s)
        {
            wr.Write(WebUtility.HtmlEncode(s));
        }

        void WriteAttributes(HtmlAttribute[] attributes)
        {
            for (int i = 0; i < attributes.Length; i++)
            {
                wr.Write(' ');
                wr.Write(attributes[i].Name);

                string val = attributes[i].Value;

                if (val == null) continue;

                val = WebUtility.HtmlEncode(val);
                wr.Write('=');
                wr.Write('"');
                wr.Write(val);
                wr.Write('"');
            }
        }

        /// <summary>
        /// Writes the opening tag for the specified HTML element, optionally including attributes
        /// </summary>
        /// <exception cref="ArgumentException">Name is null or empty string</exception>
        public void WriteOpeningTag(string name, HtmlAttribute[] attributes)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name should not be null or empty string");
            }

            if (attributes == null) attributes = NoAttributes;

            wr.Write('<');
            wr.Write(name);
            WriteAttributes(attributes);
            wr.Write('>');
        }

        /// <summary>
        /// Writes the opening tag for the specified HTML element
        /// </summary>
        /// <exception cref="ArgumentException">Name is null or empty string</exception>
        public void WriteOpeningTag(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name should not be null or empty string");
            }

            this.WriteOpeningTag(name, NoAttributes);
        }

        /// <summary>
        /// Writes the closing tag for the specified HTML element
        /// </summary>
        /// <exception cref="ArgumentException">Name is null or empty string</exception>
        public void WriteClosingTag(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name should not be null or empty string");
            }

            wr.Write('<');
            wr.Write('/');
            wr.Write(name);
            wr.Write('>');
        }

        /// <summary>
        /// Writes the specified HTML element's full markup
        /// </summary>
        /// <param name="name">Element name</param>
        /// <param name="content">Element content, or an empty string if there's no content</param>
        /// <remarks>
        /// If <paramref name="content"/> is non-empty, writes opening tag, content, and then closing tag. If it is null or 
        /// empty string, writes a self-closing tag.
        /// </remarks>
        /// <exception cref="ArgumentException">Name is null or empty string</exception>
        public void WriteElement(string name, string content)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name should not be null or empty string");
            }

            this.WriteElement(name, content, NoAttributes, false);
        }

        /// <summary>
        /// Writes the specified HTML element's full markup, optionally including attributes
        /// </summary>
        /// <param name="name">Element name</param>
        /// <param name="content">Element content, or an empty string if there's no content</param>
        /// <param name="attributes">Array of attributes</param>
        /// <remarks>
        /// If <paramref name="content"/> is non-empty, writes opening tag, content, and then closing tag. If it is null or 
        /// empty string, writes a self-closing tag.
        /// </remarks>
        /// <exception cref="ArgumentException">Name is null or empty string</exception>
        public void WriteElement(string name, string content, HtmlAttribute[] attributes)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name should not be null or empty string");
            }

            this.WriteElement(name, content, attributes, false);
        }

        /// <summary>
        /// Writes the specified HTML element's full markup, optionally including attributes
        /// </summary>
        /// <param name="name">Element name</param>
        /// <param name="content">Element content, or an empty string if there's no content</param>
        /// <param name="attributes">Array of attributes</param>
        /// <param name="isRaw">
        /// <c>true</c> to write content unescaped, <c>false</c> to escape special characters in content
        /// </param>
        /// <remarks>
        /// If <paramref name="content"/> is non-empty, writes opening tag, content, and then closing tag. If it is null or 
        /// empty string, writes a self-closing tag.
        /// </remarks>
        /// <exception cref="ArgumentException">Name is null or empty string</exception>
        public void WriteElement(string name, string content, HtmlAttribute[] attributes, bool isRaw)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name should not be null or empty string");
            }

            if (content == null) content = string.Empty;
            if (attributes == null) attributes = NoAttributes;

            wr.Write('<');
            wr.Write(name);
            WriteAttributes(attributes);

            if (content.Length == 0)
            {
                wr.Write('/');
                wr.Write('>');
                return;
            }

            wr.Write('>');
            string to_write = content;

            if (!isRaw) to_write = WebUtility.HtmlEncode(to_write);

            wr.Write(to_write);
            WriteClosingTag(name);
        }

        public void WriteHyperlink(string url, string text)
        {
            this.WriteElement("a", text, new HtmlAttribute[] { new HtmlAttribute("href", url) });
        }

        public void StartParagraph()
        {
            this.WriteOpeningTag("p", NoAttributes);
        }

        public void EndParagraph()
        {
            this.WriteClosingTag("p");
        }

        public void WriteLineBreak()
        {
            this.WriteElement("br", string.Empty, NoAttributes);
        }
    }

    internal class HtmlAttribute
    {
        public HtmlAttribute()
        {
            this.Value = string.Empty;
        }

        public HtmlAttribute(string name, string val)
        {
            this.Name = name;
            this.Value = val;
        }

        public string Name { get; set; }
        public string Value { get; set; }
    }
}
