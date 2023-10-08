﻿/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using CilTools.SourceCode.Common;
using CilTools.Syntax;

namespace CilTools.Visualization
{
    public class SyntaxVisualizer
    {
        List<UrlProviderBase> _urlProviders = new List<UrlProviderBase>();

        const string GlobalStyles = ".memberid { color: rgb(43, 145, 175); text-decoration: none; }";

        /// <summary>
        /// Adds the specified custom URL provider into the list of providers used by this generator. URL providers 
        /// define logic how URLs for navigation hyperlinks are rendered.
        /// </summary>
        public void AddUrlProvider(UrlProviderBase provider)
        {
            this._urlProviders.Add(provider);
        }

        public void RemoveAllProviders()
        {
            this._urlProviders.Clear();
        }

        string ResolveMemberUrl(MemberInfo member)
        {
            if (this._urlProviders.Count == 0) return string.Empty;

            for (int i = 0; i < this._urlProviders.Count; i++)
            {
                string url = this._urlProviders[i].GetMemberUrl(member);

                if (!string.IsNullOrEmpty(url)) return url;
            }

            return string.Empty;
        }
        
        static void RenderSourceToken(SourceToken token, HtmlBuilder target)
        {
            switch (token.Kind)
            {
                case TokenKind.Keyword:
                    target.WriteElement("span", token.ToString(), HtmlBuilder.OneAttribute("style", "color: blue;"));
                    break;
                case TokenKind.DoubleQuotLiteral:
                    target.WriteElement("span", token.ToString(), HtmlBuilder.OneAttribute("style", "color: red;"));
                    break;
                case TokenKind.SingleQuotLiteral:
                    target.WriteElement("span", token.ToString(), HtmlBuilder.OneAttribute("style", "color: red;"));
                    break;
                case TokenKind.SpecialTextLiteral:
                    target.WriteElement("span", token.ToString(), HtmlBuilder.OneAttribute("style", "color: red;"));
                    break;
                case TokenKind.Comment:
                    target.WriteElement("span", token.ToString(), HtmlBuilder.OneAttribute("style", "color: green;"));
                    break;
                case TokenKind.MultilineComment:
                    target.WriteElement("span", token.ToString(), HtmlBuilder.OneAttribute("style", "color: green;"));
                    break;
                default:
                    target.WriteEscaped(token.ToString());
                    break;
            }
        }

        protected virtual void RenderNode(SyntaxNode node, TextWriter target)
        {
            HtmlBuilder builder = new HtmlBuilder(target);
            SyntaxNode[] children = node.GetChildNodes();
            HtmlAttribute[] attrs;

            if (children.Length > 0)
            {
                foreach (SyntaxNode child in children) this.RenderNode(child, target);
            }
            else if (node is KeywordSyntax)
            {
                KeywordSyntax ks = (KeywordSyntax)node;

                if (ks.Kind == KeywordKind.DirectiveName)
                {
                    attrs = new HtmlAttribute[1];
                    attrs[0] = new HtmlAttribute("style", "color: magenta;");
                }
                else if (ks.Kind == KeywordKind.Other)
                {
                    attrs = new HtmlAttribute[1];
                    attrs[0] = new HtmlAttribute("style", "color: blue;");
                }
                else attrs = new HtmlAttribute[0];

                builder.WriteElement("span", node.ToString(), attrs);
            }
            else if (node is IdentifierSyntax)
            {
                IdentifierSyntax ids = (IdentifierSyntax)node;
                string tagName;
                List<HtmlAttribute> attrList = new List<HtmlAttribute>(5);

                if (ids.IsMemberName)
                {
                    if (ids.TargetMember is Type)
                    {
                        Type targetType = (Type)ids.TargetMember;
                        string urlResolved = this.ResolveMemberUrl(targetType);

                        if (!string.IsNullOrEmpty(urlResolved))
                        {
                            //hyperlink if URL is resolved via providers
                            tagName = "a";
                            attrList.Add(new HtmlAttribute("href", urlResolved));
                        }
                        else
                        {
                            //plaintext name for other types
                            tagName = "span";
                        }
                    }
                    else if (ids.TargetMember is MethodBase)
                    {
                        MethodBase mb = (MethodBase)ids.TargetMember;
                        string urlResolved = this.ResolveMemberUrl(mb);

                        if (!string.IsNullOrEmpty(urlResolved))
                        {
                            //hyperlink if URL is resolved via providers
                            tagName = "a";
                            attrList.Add(new HtmlAttribute("href", urlResolved));
                        }
                        else
                        {
                            //plaintext name for other methods
                            tagName = "span";
                        }
                    }
                    else //other members
                    {
                        string urlResolved = this.ResolveMemberUrl(ids.TargetMember);

                        if (!string.IsNullOrEmpty(urlResolved))
                        {
                            //hyperlink if URL is resolved via providers
                            tagName = "a";
                            attrList.Add(new HtmlAttribute("href", urlResolved));
                        }
                        else
                        {
                            //plaintext name for other members
                            tagName = "span";
                        }
                    }

                    attrList.Add(new HtmlAttribute("class", "memberid"));
                }
                else tagName = "span";

                builder.WriteElement(tagName, node.ToString(), attrList.ToArray());
            }
            else if (node is LiteralSyntax)
            {
                LiteralSyntax ls = (LiteralSyntax)node;

                if (ls.Value is string)
                {
                    attrs = new HtmlAttribute[1];
                    attrs[0] = new HtmlAttribute("style", "color: red;");
                }
                else attrs = new HtmlAttribute[0];

                builder.WriteElement("span", node.ToString(), attrs);
            }
            else if (node is CommentSyntax)
            {
                attrs = new HtmlAttribute[1];
                attrs[0] = new HtmlAttribute("style", "color: green;");
                builder.WriteElement("span", node.ToString(), attrs);
            }
            else if (node is SourceToken)
            {
                SourceToken token = (SourceToken)node;
                RenderSourceToken(token, builder);
            }
            else
            {
                builder.WriteEscaped(node.ToString());
            }
        }

        /// <summary>
        /// Visualizes the specified collection of syntax nodes as HTML and writes result into HtmlBuilder
        /// </summary>
        /// <param name="nodes">Collection of nodes to visualize</param>
        /// <param name="target">HtmlBuilder to write resulting HTML</param>
        internal void VisualizeSyntaxNodes(IEnumerable<SyntaxNode> nodes, HtmlBuilder target)
        {
            target.WriteOpeningTag("pre", HtmlBuilder.OneAttribute("style", "white-space: pre-wrap;"));
            target.WriteOpeningTag("code");

            //content
            foreach (SyntaxNode node in nodes) this.RenderNode(node, target.Target);

            target.WriteClosingTag("code");
            target.WriteClosingTag("pre");
        }

        /// <summary>
        /// Renders the specified collection of syntax nodes as HTML with syntax highlighting and writes the resulting markup into 
        /// <c>TextWriter</c>
        /// </summary>
        /// <param name="nodes">Collection of nodes to render</param>
        /// <param name="target">TextWriter to write resulting HTML</param>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is null</exception>
        public void RenderSyntaxNodes(IEnumerable<SyntaxNode> nodes, TextWriter target)
        {
            if (nodes == null) return;
            if (target == null) throw new ArgumentNullException("target");

            this.VisualizeSyntaxNodes(nodes, new HtmlBuilder(target));
            target.Flush();
        }

        /// <summary>
        /// Renders the specified collection of syntax nodes as HTML with syntax highlighting and returns the string with 
        /// resulting markup
        /// </summary>
        /// <param name="nodes">Collection of nodes to render</param>
        public string RenderSyntaxNodes(IEnumerable<SyntaxNode> nodes)
        {
            if (nodes == null) return string.Empty;

            StringBuilder sb = new StringBuilder();
            this.VisualizeSyntaxNodes(nodes, new HtmlBuilder(sb));
            return sb.ToString();
        }

        /// <summary>
        /// Writes CSS code containing syntax highlighting styles into the specified TextWriter. 
        /// These styles are used in markup generated by <see cref="RenderSourceText"/> method.
        /// </summary>
        /// <param name="target">Text writer where to output the resulting CSS</param>
        public static void GetVisualStyles(TextWriter target)
        {
            if (target == null) throw new ArgumentNullException("target");

            target.WriteLine(GlobalStyles);
            target.Flush();
        }

        /// <summary>
        /// Gets CSS code containing syntax highlighting styles. These styles are used in markup generated by 
        /// <see cref="RenderSourceText"/> method.
        /// </summary>
        public static string GetVisualStyles()
        {
            return GlobalStyles;
        }
    }
}
