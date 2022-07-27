/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.CommandLine
{
    class TextFragment
    {
        public TextFragment(string text, bool isCode)
        {
            this.Text = text;
            this.IsCode = isCode;
        }

        public string Text { get; set; }

        public bool IsCode { get; set; }
    }

    class TextParagraph
    {
        List<TextFragment> fragments = new List<TextFragment>();

        public static TextParagraph FromCollection(IEnumerable<TextFragment> coll)
        {
            TextParagraph par = new TextParagraph();

            foreach (TextFragment fragment in coll)
            {
                par.Add(fragment);
            }

            return par;
        }

        public static TextParagraph FromFragment(TextFragment fragment)
        {
            TextParagraph par = new TextParagraph();
            par.Add(fragment);
            return par;
        }

        public static TextParagraph Text(string text)
        {
            return FromFragment(new TextFragment(text, false));
        }

        public static TextParagraph Code(string text)
        {
            return FromFragment(new TextFragment(text, true));
        }

        public void Add(TextFragment fragment)
        {
            this.fragments.Add(fragment);
        }

        public void Clear()
        {
            this.fragments.Clear();
        }

        public IEnumerable<TextFragment> Fragments
        {
            get { return this.fragments.ToArray(); }
        }

        public string GetText()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < this.fragments.Count; i++)
            {
                sb.Append(this.fragments[i].Text);
            }

            return sb.ToString();
        }

        static string EscapeForMarkdown(string str)
        {
            string ret = str;
            ret = ret.Replace("<", "\\<");
            ret = ret.Replace(">", "\\>");
            return ret;
        }

        public string GetMarkdown()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < this.fragments.Count; i++)
            {
                if (this.fragments[i].IsCode)
                {
                    sb.Append(this.fragments[i].Text);
                }
                else
                {
                    sb.Append(EscapeForMarkdown(this.fragments[i].Text));
                }
            }

            return sb.ToString();
        }
    }
}
