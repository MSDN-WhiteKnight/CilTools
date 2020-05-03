/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using CilTools.BytecodeAnalysis;

namespace CilTools.Syntax
{
    public class DirectiveNode:SyntaxTreeNode
    {
        string _lead;
        string _name;
        string _content;
        BlockNode _child;

        internal DirectiveNode(string lead,string name, string content, BlockNode child)
        {
            if (lead == null) lead = "";
            if (content == null) content = "";

            this._lead = lead;
            this._name = name;
            this._content = content;
            this._child = child;
        }

        public override IEnumerable<SyntaxTreeNode> Children
        {
            get 
            {
                if (this._child != null)
                {
                    yield return this._child;
                }

                yield break;
            }
        }

        public override void ToText(TextWriter target)
        {
            target.Write(this._lead);
            target.Write('.');
            target.Write(this._name);
            target.Write(this._content);

            if (this._child != null)
            {
                target.Write(' ');
                this._child.ToText(target);
            }

            target.Flush();
        }
    }
}
