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
using System.Globalization;

namespace CilTools.Syntax
{
    public class LiteralSyntax:SyntaxNode
    {
        object _value;

        public object Value { get { return this._value; } }

        internal LiteralSyntax(string lead, object value, string trail)
        {
            if (lead == null) lead = "";
            if (trail == null) trail = "";

            this._lead = lead;
            this._value = value;
            this._trail = trail;
        }

        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            target.Write(this._lead);
            if (this._value is string)
            {
                target.Write('"');
                target.Write(CilAnalysis.EscapeString((string)this._value));
                target.Write('"');
            }
            else target.Write(Convert.ToString(this._value, CultureInfo.InvariantCulture));
            
            target.Write(this._trail);
            target.Flush();
        }

        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            return SyntaxNode.EmptyArray;
        }
    }
}
