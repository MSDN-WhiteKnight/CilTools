/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Syntax
{
    internal class InvalidSyntax : SyntaxNode
    {
        string _content;
        string _errormes;

        internal InvalidSyntax(string lead, string content, string errorMessage, string trail)
        {
            if (lead == null) lead = "";
            if (trail == null) trail = "";
            this._lead = lead;
            this._content = content;
            this._errormes = errorMessage;
            this._trail = trail;
        }

        public string Content { get { return this._content; } }

        public string ErrorMessage { get { return this._errormes; } }

        /// <inheritdoc/>
        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            target.Write(this._lead);
            target.Write(this._content);
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
