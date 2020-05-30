﻿/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CilTools.BytecodeAnalysis;

namespace CilTools.Syntax
{
    public class KeywordSyntax:SyntaxNode
    {
        string _content;

        public string Content { get { return this._content; } }

        internal KeywordSyntax(string lead, string content, string trail)
        {
            if (lead == null) lead = "";
            if (trail == null) trail = "";

            this._lead = lead;
            this._content = content;
            this._trail = trail;
        }

        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            target.Write(this._lead);
            target.Write(this._content);
            target.Write(this._trail);
            target.Flush();
        }

        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            return SyntaxNode.EmptyArray;
        }
    }
}
