/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Reflection;
using CilTools.BytecodeAnalysis;

namespace CilTools.Syntax
{
    /// <summary>
    /// Represents the syntax of literal constant (such as numeric or string) in CIL assembler
    /// </summary>
    /// <remarks>
    /// Literal constant value is inlined directly in the source code. Typically literal syntax is used to 
    /// represent the operand of the instruction.
    /// </remarks>
    public class LiteralSyntax : SyntaxNode
    {
        object _value;
        string _rawvalue;

        /// <summary>
        /// Gets the value represented by this literal
        /// </summary>
        public object Value { get { return this._value; } }
        
        LiteralSyntax(string lead, object value, string rawValue, string trail)
        {
            this._lead = lead;
            this._value = value;
            this._rawvalue = rawValue;
            this._trail = trail;
        }

        internal static LiteralSyntax CreateFromValue(string lead, object value, string trail)
        {
            if (lead == null) lead = string.Empty;
            if (trail == null) trail = string.Empty;

            return new LiteralSyntax(lead, value, GetRawValue(value), trail);
        }

        internal static LiteralSyntax CreateFromRawValue(string lead, string rawValue, string trail)
        {
            if (lead == null) lead = string.Empty;
            if (trail == null) trail = string.Empty;

            object val = null;

            if (rawValue.Length >= 2 && rawValue[0] == '"')
            {
                val = SyntaxFactory.Strip(rawValue, 1, 1);
            }

            return new LiteralSyntax(lead, val, rawValue, trail);
        }

        static string GetRawValue(object val)
        {
            StringBuilder sb = new StringBuilder();

            if (val is string)
            {
                sb.Append('"');
                sb.Append(CilAnalysis.EscapeString((string)val));
                sb.Append('"');
            }
            else sb.Append(Convert.ToString(val, CultureInfo.InvariantCulture));

            return sb.ToString();
        }

        /// <inheritdoc/>
        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            target.Write(this._lead);
            target.Write(this._rawvalue);
            target.Write(this._trail);
            target.Flush();
        }

        /// <inheritdoc/>
        public override IEnumerable<SyntaxNode> EnumerateChildNodes()
        {
            return SyntaxNode.EmptyArray;
        }
    }
}
