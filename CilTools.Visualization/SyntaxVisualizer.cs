/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.SourceCode.Common;
using CilTools.Syntax;

namespace CilTools.Visualization
{
    public class SyntaxVisualizer
    {
        Assembly _ass;
        List<UrlProviderBase> _urlProviders = new List<UrlProviderBase>();

        const string GlobalStyles = ".memberid { color: rgb(43, 145, 175); text-decoration: none; }";

        public SyntaxVisualizer()
        {
            this._ass = null;
        }

        public SyntaxVisualizer(Assembly ass)
        {
            this._ass = ass;
        }

        /// <summary>
        /// Adds the specified custom URL provider into the list of providers used by this generator. URL providers 
        /// define logic how URLs for navigation hyperlinks are rendered.
        /// </summary>
        public void AddUrlProvider(UrlProviderBase provider)
        {
            this._urlProviders.Add(provider);
        }

        string ResolveMemberUrl(MemberInfo member, int level)
        {
            if (this._urlProviders.Count == 0) return string.Empty;

            for (int i = 0; i < this._urlProviders.Count; i++)
            {
                string url = this._urlProviders[i].GetMemberUrl(member, level);

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

        void VisualizeNode(SyntaxNode node, HtmlBuilder target, int level)
        {
            SyntaxNode[] children = node.GetChildNodes();
            HtmlAttribute[] attrs;

            if (children.Length > 0)
            {
                foreach (SyntaxNode child in children) this.VisualizeNode(child, target, level);
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

                target.WriteElement("span", node.ToString(), attrs);
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
                        string urlResolved = this.ResolveMemberUrl(targetType, level);

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
                        string urlResolved = this.ResolveMemberUrl(mb, level);

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
                        string urlResolved = this.ResolveMemberUrl(ids.TargetMember, level);

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

                target.WriteElement(tagName, node.ToString(), attrList.ToArray());
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

                target.WriteElement("span", node.ToString(), attrs);
            }
            else if (node is CommentSyntax)
            {
                attrs = new HtmlAttribute[1];
                attrs[0] = new HtmlAttribute("style", "color: green;");
                target.WriteElement("span", node.ToString(), attrs);
            }
            else if (node is SourceToken)
            {
                SourceToken token = (SourceToken)node;
                RenderSourceToken(token, target);
            }
            else
            {
                target.WriteEscaped(node.ToString());
            }
        }

        protected virtual void RenderNode(SyntaxNode node, TextWriter target)
        {
            HtmlBuilder builder = new HtmlBuilder(target);
            this.VisualizeNode(node, builder, 0);
        }

        /// <summary>
        /// Visualizes the specified collection of syntax nodes as HTML and writes result into HtmlBuilder
        /// </summary>
        /// <param name="nodes">Collection of nodes to visualize</param>
        /// <param name="level">Level in website structure, when structure mode is used</param>
        /// <param name="target">HtmlBuilder to write resulting HTML</param>
        void VisualizeSyntaxNodes(IEnumerable<SyntaxNode> nodes, int level, HtmlBuilder target)
        {
            target.WriteOpeningTag("pre", HtmlBuilder.OneAttribute("style", "white-space: pre-wrap;"));
            target.WriteOpeningTag("code");

            //content
            foreach (SyntaxNode node in nodes) this.VisualizeNode(node, target, level);

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

            this.VisualizeSyntaxNodes(nodes, 0, new HtmlBuilder(target));
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
            this.VisualizeSyntaxNodes(nodes, 0, new HtmlBuilder(sb));
            return sb.ToString();
        }

        void VisualizeMethodImpl(MethodBase mb, HtmlBuilder target)
        {
            CilGraph gr = CilGraph.Create(mb);
            SyntaxNode[] nodes = new SyntaxNode[] { gr.ToSyntaxTree() };
            this.VisualizeSyntaxNodes(nodes, 1, target);
        }

        /// <summary>
        /// Renders IL of the specified method as HTML with syntax highlighting and writes the resulting markup into 
        /// <c>TextWriter</c>
        /// </summary>
        public void RenderMethod(MethodBase method, TextWriter target)
        {
            if (method == null) throw new ArgumentNullException("method");
            if (target == null) throw new ArgumentNullException("target");

            HtmlBuilder builder = new HtmlBuilder(target);
            this.VisualizeMethodImpl(method, builder);
            target.Flush();
        }

        /// <summary>
        /// Renders IL of the specified method as HTML with syntax highlighting and returns the string with resulting markup
        /// </summary>
        public string RenderMethod(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException("method");

            StringBuilder sb = new StringBuilder(5000);
            HtmlBuilder builder = new HtmlBuilder(sb);
            this.VisualizeMethodImpl(method, builder);
            return sb.ToString();
        }

        void VisualizeTypeImpl(Type t, bool full, HtmlBuilder target)
        {
            SyntaxNode[] nodes = SyntaxNode.GetTypeDefSyntax(t, full, new DisassemblerParams()).ToArray();

            if (nodes.Length == 0) return;

            if (nodes.Length == 1)
            {
                if (string.IsNullOrWhiteSpace(nodes[0].ToString())) return;
            }

            this.VisualizeSyntaxNodes(nodes, 1, target);
        }

        /// <summary>
        /// Renders IL of the specified type as HTML with syntax highlighting and writes the resulting markup into 
        /// <c>TextWriter</c>
        /// </summary>
        public void RenderType(Type t, bool full, TextWriter target)
        {
            if (t == null) throw new ArgumentNullException("t");
            if (target == null) throw new ArgumentNullException("target");

            HtmlBuilder html = new HtmlBuilder(target);
            this.VisualizeTypeImpl(t, full, html);
            target.Flush();
        }

        /// <summary>
        /// Renders IL of the specified type as HTML with syntax highlighting and returns string with resulting markup
        /// </summary>
        public string RenderType(Type t, bool full)
        {
            if (t == null) throw new ArgumentNullException("t");

            StringBuilder sb = new StringBuilder();
            HtmlBuilder html = new HtmlBuilder(sb);
            this.VisualizeTypeImpl(t, full, html);
            return sb.ToString();
        }

        void VisualizeAssemblyManifestImpl(Assembly ass, HtmlBuilder target)
        {
            IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);
            this.VisualizeSyntaxNodes(nodes, 0, target);
        }

        /// <summary>
        /// Renders IL of the specified assembly's manifest as HTML with syntax highlighting and writes the resulting 
        /// markup into <c>TextWriter</c>
        /// </summary>
        /// <exception cref="ArgumentNullException">Assembly or target <c>TextWriter</c> is null</exception>
        public void RenderAssemblyManifest(Assembly ass, TextWriter target)
        {
            if (ass == null) throw new ArgumentNullException("ass");
            if (target == null) throw new ArgumentNullException("target");

            HtmlBuilder html = new HtmlBuilder(target);
            this.VisualizeAssemblyManifestImpl(ass, html);
            target.Flush();
        }

        /// <summary>
        /// Renders IL of the specified assembly's manifest as HTML with syntax highlighting and returns string with 
        /// resulting markup
        /// </summary>
        /// <exception cref="ArgumentNullException">Assembly is null</exception>
        public string RenderAssemblyManifest(Assembly ass)
        {
            if (ass == null) throw new ArgumentNullException("ass");

            StringBuilder sb = new StringBuilder();
            HtmlBuilder html = new HtmlBuilder(sb);
            this.VisualizeAssemblyManifestImpl(ass, html);
            return sb.ToString();
        }

        void VisualizeSourceTextImpl(string content, string ext, HtmlBuilder html)
        {
            //convert source text into tokens
            SyntaxNodeCollection coll = SourceParser.Parse(content, ext);

            //convert tokens to HTML
            this.VisualizeSyntaxNodes(coll.GetChildNodes(), 0, html);
        }

        /// <summary>
        /// Generates HTML markup with syntax highlighting for the specified source text and writes it into TextWriter. 
        /// </summary>
        /// <param name="sourceText">Source text to render</param>
        /// <param name="ext">
        /// Source file extension (with leading dot) that defines programming language. For example, <c>.cs</c> for C#.
        /// </param>
        /// <param name="target">Text writer where to output the resulting HTML</param>
        public static void RenderSourceText(string sourceText, string ext, TextWriter target)
        {
            if (string.IsNullOrEmpty(sourceText)) return;

            if (target == null) throw new ArgumentNullException("target");

            SyntaxVisualizer vis = new SyntaxVisualizer();
            HtmlBuilder builder = new HtmlBuilder(target);
            vis.VisualizeSourceTextImpl(sourceText, ext, builder);
            target.Flush();
        }

        /// <summary>
        /// Generates HTML markup with syntax highlighting for the specified source text.
        /// </summary>
        /// <param name="sourceText">Source text to render</param>
        /// <param name="ext">
        /// Source file extension (with leading dot) that defines programming language. For example, <c>.cs</c> for C#.
        /// </param>
        public static string RenderSourceText(string sourceText, string ext)
        {
            if (string.IsNullOrEmpty(sourceText)) return string.Empty;

            StringBuilder sb = new StringBuilder(sourceText.Length * 2);
            StringWriter wr = new StringWriter(sb);
            SyntaxVisualizer vis = new SyntaxVisualizer();
            HtmlBuilder builder = new HtmlBuilder(wr);
            vis.VisualizeSourceTextImpl(sourceText, ext, builder);
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
