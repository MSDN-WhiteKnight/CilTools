/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Syntax
{
    /// <summary>
    /// Represents the comment in the CIL assembler code. 
    /// The comment does not impact code semantics in any way, but may provide extra information.
    /// </summary>
    public class CommentSyntax:SyntaxNode
    {
        string _content;
        string _rawContent;

        CommentSyntax(string lead, string content, string rawContent, string trail)
        {
            this._lead = lead;
            this._content = content;
            this._rawContent = rawContent;
            this._trail = trail;
        }

        internal static CommentSyntax Create(string lead, string content, string trail, bool isRaw)
        {
            if (lead == null) lead = string.Empty;
            if (trail == null) trail = Environment.NewLine;

            if (isRaw)
            {
                //content represents raw syntax token
                string contentParsed = null;

                if (content.StartsWith("//")) contentParsed = SyntaxFactory.Strip(content, 2, 0);
                else if (content.StartsWith("/*")) contentParsed = SyntaxFactory.Strip(content, 2, 2);

                return new CommentSyntax(lead, contentParsed, content, trail);
            }
            else
            {
                //content represents inner text inside a comment
                return new CommentSyntax(lead, content, GetRawContent(content), trail);
            }
        }

        static string GetRawContent(string content)
        {
            return "//" + content;
        }

        /// <inheritdoc/>
        public override void ToText(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            target.Write(this._lead);            
            target.Write(this._rawContent);
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
