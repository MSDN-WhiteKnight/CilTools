/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CilTools.Syntax;
using CilView.Common;

namespace CilView.Core.Syntax
{
    public static class IlasmParser
    {
        /// <summary>
        /// Transforms tokens sequence into an initial syntax tree shape, where every block ({...}) is 
        /// represented as a subnode
        /// </summary>
        public static SyntaxNode TokensToInitialTree(IEnumerable<SyntaxNode> tokens)
        {
            SyntaxSequence root=new SyntaxSequence();
            List<SyntaxSequence> currentPath = new List<SyntaxSequence>(10);
            SyntaxSequence currNode = root;
            SyntaxSequence newNode;
            
            foreach (SyntaxNode token in tokens)
            {
                if (token is PunctuationSyntax)
                {
                    if (Utils.StringEquals(((PunctuationSyntax)token).Content, "}"))
                    {
                        if (currentPath.Count == 0)
                        {
                            throw new Exception("Unexpected closing brace");
                        }

                        newNode = currentPath[currentPath.Count - 1];
                        currentPath.RemoveAt(currentPath.Count - 1);

                        if (currentPath.Count > 0) currNode = currentPath[currentPath.Count - 1];
                        else currNode = root;

                        newNode.Add(token);
                        currNode.Add(newNode);
                    }
                    else if (Utils.StringEquals(((PunctuationSyntax)token).Content, "{"))
                    {
                        newNode = new SyntaxSequence();
                        newNode.Add(token);
                        currentPath.Add(newNode);
                        currNode = newNode;
                    }
                    else
                    {
                        currNode.Add(token);
                    }
                }
                else
                {
                    currNode.Add(token);
                }
            }

            return root;
        }

        class SyntaxSequence : SyntaxNode
        {
            List<SyntaxNode> _children;

            public SyntaxSequence(IEnumerable<SyntaxNode> children)
            {
                this._children = new List<SyntaxNode>(children);
            }

            public SyntaxSequence()
            {
                this._children = new List<SyntaxNode>(100);
            }

            public void Add(SyntaxNode node)
            {
                this._children.Add(node);
            }

            public override IEnumerable<SyntaxNode> EnumerateChildNodes()
            {
                return this._children.ToArray();
            }

            public override void ToText(TextWriter target)
            {
                for (int i = 0; i < _children.Count; i++)
                {
                    _children[i].ToText(target);
                }

                target.Flush();
            }
        }
    }
}
