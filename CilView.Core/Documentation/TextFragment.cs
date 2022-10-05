/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilView.Core.Documentation
{
    public class TextFragment
    {
        public TextFragment(string text, bool isCode)
        {
            this.Text = text;
            this.IsCode = isCode;
        }

        public string Text { get; set; }

        public bool IsCode { get; set; }
    }
}
