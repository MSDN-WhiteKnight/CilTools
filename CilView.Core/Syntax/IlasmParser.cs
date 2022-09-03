/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CilTools.Syntax;
using CilView.Common;
using CilView.Core.DocumentModel;

namespace CilView.Core.Syntax
{
    public static class IlasmParser
    {
        /// <summary>
        /// Transforms tokens sequence into an initial syntax tree shape, where every block ({...}) is 
        /// represented as a subnode
        /// </summary>
        public static DocumentSyntax TokensToInitialTree(IEnumerable<SyntaxNode> tokens)
        {
            DocumentSyntax root=new DocumentSyntax("(All text)");
            List<DocumentSyntax> currentPath = new List<DocumentSyntax>(10);
            DocumentSyntax currNode = root;
            DocumentSyntax newNode;
            
            foreach (SyntaxNode token in tokens)
            {
                if (token is PunctuationSyntax)
                {
                    if (Utils.StringEquals(((PunctuationSyntax)token).Content, "}"))
                    {
                        if (currentPath.Count == 0)
                        {
                            //invalid syntax!
                            return new DocumentSyntax(tokens, "(All text)", true, "Unexpected closing brace");
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
                        newNode = new DocumentSyntax(string.Empty);
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

        /// <summary>
        /// Transforms the syntax tree so every top level directive (.assembly, .class) with its content 
        /// is represented as subnode (second stage of parsing).
        /// </summary>
        public static DocumentSyntax ParseTopLevelDirectives(DocumentSyntax tree)
        {
            DocumentSyntax ret = new DocumentSyntax(tree.Name, tree.IsInvalid, tree.ParserDiagnostics);
            SyntaxNode[] nodes = tree.GetChildNodes();
            bool inDir = false;
            DocumentSyntax currNode = ret;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] is KeywordSyntax && ((KeywordSyntax)nodes[i]).Kind == KeywordKind.DirectiveName)
                {
                    if (inDir)
                    {
                        //end of directive
                        ret.Add(currNode); //add previous directive
                        currNode = new DocumentSyntax(((KeywordSyntax)nodes[i]).Content);
                        currNode.Add(nodes[i]);
                    }
                    else
                    {
                        //start of first directive
                        currNode = new DocumentSyntax(((KeywordSyntax)nodes[i]).Content);
                        currNode.Add(nodes[i]);
                        inDir = true;
                    }
                }
                else
                {
                    currNode.Add(nodes[i]);
                }
            }

            if (inDir)
            {
                //end of last directive
                ret.Add(currNode);
            }

            return ret;
        }

        static string FindFirstIdentifier(DocumentSyntax ds)
        {
            // .class ... Foo ... {

            foreach (SyntaxNode node in ds.EnumerateChildNodes())
            {
                if (node is IdentifierSyntax)
                {
                    return ((IdentifierSyntax)node).Content;
                }
                else if (node is PunctuationSyntax)
                {
                    if (Utils.StringEquals(((PunctuationSyntax)node).Content, "{")) break;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Transforms syntax tree into a synthesized assembly with a collection of types (third stage of parsing).
        /// </summary>
        public static IlasmAssembly TreeToAssembly(DocumentSyntax tree)
        {
            IlasmAssembly ret = new IlasmAssembly(tree, "IlasmAssembly");
            SyntaxNode[] nodes = tree.GetChildNodes();

            for (int i = 0; i < nodes.Length; i++)
            {
                if (!(nodes[i] is DocumentSyntax)) continue; //not interested in simple nodes

                DocumentSyntax ds = (DocumentSyntax)nodes[i];

                if (Utils.StringEquals(ds.Name, ".class"))
                {
                    string typeName = FindFirstIdentifier(ds);

                    if (string.IsNullOrEmpty(typeName)) typeName = "Type" + i.ToString();

                    ret.AddType(new IlasmType(ret, ds, typeName));
                }
            }

            return ret;
        }
    }
}
