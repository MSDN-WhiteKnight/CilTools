/* CIL Tools 
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
    public class VarDeclSyntax:SyntaxElement
    {
        string _type;
        string _name;
        
        public string Type { get { return this._type; } }
        public string Name { get { return this._name; } }

        internal VarDeclSyntax(string type, string name)
        {
            if (name == null) name = "";

            this._type = type;
            this._name = name;
        }
        
        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            target.Write(this._lead);
            target.Write(this._type);
            target.Write(' ');
            target.Write(this._name);
            target.Flush();
        }
    }
}
