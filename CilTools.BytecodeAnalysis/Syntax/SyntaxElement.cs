/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Syntax
{
    public abstract class SyntaxElement
    {
        protected string _lead;

        public abstract void ToText(TextWriter target);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(60);
            StringWriter wr = new StringWriter(sb);
            this.ToText(wr);
            wr.Flush();
            return sb.ToString();
        }
    }
}
