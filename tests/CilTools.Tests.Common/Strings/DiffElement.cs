/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Net;
using System.Text;

namespace CilTools.Tests.Common
{
    public enum DiffKind
    {
        Unknown = 0, Addition = 1, Deletion, Same
    }

    public class DiffElement
    {
        public DiffElement(DiffKind kind, string val)
        {
            this.Kind = kind;
            this.Value = val;
        }

        public DiffKind Kind { get; set; }
        public string Value { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if(!(obj is DiffElement)) return false;

            DiffElement de = (DiffElement)obj;
            return de.Kind == this.Kind && string.Equals(de.Value, this.Value, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            if (this.Value == null) return (int)this.Kind;

            int x = (int)this.Kind;
            int y = this.Value.GetHashCode();
            unchecked { return x * 1000 + y; }
        }

        public string Visualize()
        {
            if (this.Kind == DiffKind.Same)
            {
                return this.Value;
            }

            string prefix = string.Empty;

            if (this.Kind == DiffKind.Addition) prefix = "+";
            else if (this.Kind == DiffKind.Deletion) prefix = "-";

            if (!this.Value.Contains("\n"))
            {
                return " " + prefix + "[" + this.Value + "] ";
            }

            string normalized = this.Value.Replace("\r\n", "\n");
            string[] lines = normalized.Split('\n');

            StringBuilder sb = new StringBuilder(this.Value.Length);

            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine();
                sb.Append(prefix);
                sb.Append(lines[i]);
            }

            sb.AppendLine();
            return sb.ToString();
        }

        public void VisualizeHTML(TextWriter wr)
        {
            if (this.Kind == DiffKind.Same)
            {
                wr.Write(WebUtility.HtmlEncode(this.Value));
                wr.Flush();
                return;
            }

            string prefix = string.Empty;
            string color = "black";

            if (this.Kind == DiffKind.Addition)
            {
                prefix = "+";
                color = "green";
            }
            else if (this.Kind == DiffKind.Deletion)
            {
                prefix = "-";
                color = "red";
            }

            if (!this.Value.Contains("\n"))
            {
                wr.Write("<span style=\"color:" + color +"\">");
                wr.Write(WebUtility.HtmlEncode(prefix + this.Value));
                wr.Write("</span>");
                wr.Flush();
                return;
            }

            string normalized = this.Value.Replace("\r\n", "\n");
            string[] lines = normalized.Split('\n');
                        
            for (int i = 0; i < lines.Length; i++)
            {
                wr.WriteLine();
                wr.Write("<span style=\"color:" + color + "\">");
                wr.Write(prefix);
                wr.Write(WebUtility.HtmlEncode(lines[i]));
                wr.Write("</span>");
            }

            wr.WriteLine();
            wr.Flush();
        }
    }
}
