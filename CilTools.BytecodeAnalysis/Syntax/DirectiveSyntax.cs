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
    public class DirectiveSyntax:SyntaxElement
    {        
        string _name;
        string _content;        
                
        public string Name { get { return this._name; } }
        public string Content { get { return this._content; } }        

        internal DirectiveSyntax(string lead,string name, string content)
        {
            if (lead == null) lead = "";
            if (content == null) content = "";            

            this._lead = lead;
            this._name = name;
            this._content = content;            
        }

        public override void ToText(TextWriter target)
        {
            this.WriteLead(target);
            target.Write('.');
            target.Write(this._name);
            target.Write(' ');
            target.Write(this._content);
            target.Flush();
        }

        public void WriteLead(TextWriter target)
        {
            target.Write(this._lead);
        }
    }
}
