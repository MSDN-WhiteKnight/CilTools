/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
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
    }
}
