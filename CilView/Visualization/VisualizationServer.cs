/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilTools.Syntax;
using CilTools.Visualization;
using CilView.Common;
using CilView.Core.DocumentModel;

namespace CilView.Visualization
{
    /// <summary>
    /// Generates HTML to visualize metadata object identified by URL
    /// </summary>
    class VisualizationServer : ServerBase
    {
        AssemblySource _src;
        HtmlVisualizer _vis;
        CilViewUrlProvider _provider;

        public VisualizationServer(string urlHost, string urlPrefix) : base(urlHost, urlPrefix)
        {
            this._vis = new HtmlVisualizer();
            this._provider = new CilViewUrlProvider();
            this._vis.AddUrlProvider(this._provider);
        }

        public AssemblySource Source
        {
            get { return this._src; }
            set { this._src = value; }
        }
        
        static MemberInfo ResolveMember(Assembly ass, int metadataToken)
        {
            if (ass is ITokenResolver)
            {
                ITokenResolver resolver = (ITokenResolver)ass;
                return resolver.ResolveMember(metadataToken);
            }
            else
            {
                return ass.ManifestModule.ResolveMember(metadataToken);
            }
        }

        Assembly ResolveAssembly(string assemblyName)
        {
            if (this._src == null) return null;
            
            return this._src.GetAssembly(assemblyName);
        }
        
        protected override void OnStart() { }

        protected override void RenderFrontPage(HttpListenerResponse response)
        {
            // Not implemented
        }
        
        public static string PrepareContent(string content)
        {
            StringBuilder sb = new StringBuilder();
            HtmlBuilder html = new HtmlBuilder(sb);
            html.WriteOpeningTag("html");
            html.WriteOpeningTag("head");

            // CSS only works good starting from Internet Explorer 9
            html.WriteRaw("<meta http-equiv=\"X-UA-Compatible\" content=\"IE=9\"/>");

            html.WriteElement("style", HtmlVisualizer.GetVisualStyles());
            html.WriteOpeningTag("head");
            html.WriteOpeningTag("body");
            html.WriteRaw(content);
            html.WriteClosingTag("body");
            html.WriteClosingTag("html");
            return sb.ToString();
        }

        static string GetInstructionAnchor(MethodBase mb, uint offset)
        {
            if (mb == null) return offset.ToString(CultureInfo.InvariantCulture);

            int token = 0;

            try { token = mb.MetadataToken; }
            catch (InvalidOperationException) { }

            string tokenStr;
            if (token != 0) tokenStr = token.ToString("X", CultureInfo.InvariantCulture) + "_";
            else tokenStr = string.Empty;

            return tokenStr + offset.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets HTML that visualizes object using the specified visualizer
        /// </summary>
        public static string VisualizeObject(object obj, HtmlVisualizer vis, VisualizationOptions options)
        {
            if (obj is IlasmAssembly)
            {
                //synthesized assembly that contains IL - no need to disassemble
                IlasmAssembly ia = (IlasmAssembly)obj;
                string html = vis.RenderToString(ia.Syntax.GetChildNodes());
                return PrepareContent(html);
            }
            else if (obj is IlasmType)
            {
                //synthesized type that contains IL - no need to disassemble
                IlasmType dt = (IlasmType)obj;
                string html = vis.RenderToString(dt.Syntax.GetChildNodes());
                return PrepareContent(html);
            }
            else if (obj is Assembly)
            {
                //assembly manifest
                Assembly ass = (Assembly)obj;
                IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);
                string html = vis.RenderToString(nodes);
                return PrepareContent(html);
            }
            else if (obj is Type)
            {
                //type disassembled IL
                Type t = (Type)obj;
                IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, full: false, new DisassemblerParams());
                string html = vis.RenderToString(nodes);
                return PrepareContent(html);
            }
            else if (obj is MethodBase)
            {
                //method disassembled IL
                MethodBase mb = (MethodBase)obj;
                CilGraph gr = CilGraph.Create(mb);
                MethodDefSyntax mds = gr.ToSyntaxTree(CilVisualization.CurrentDisassemblerParams);
                string rendered = vis.RenderToString(mds.EnumerateChildNodes(), options);

                if (options.HighlightingStartOffset >= 0)
                {
                    // Add script to jump to the first highlighted instruction on page load
                    string url = "#" + GetInstructionAnchor(mb, (uint)options.HighlightingStartOffset);
                    rendered += "<script type=\"text/javascript\">location.href='" + url + "';</script>";
                }

                return PrepareContent(rendered);
            }
            else return string.Empty;
        }

        /// <summary>
        /// Gets HTML that visualizes object using CIL View visualizer
        /// </summary>
        public string Visualize(object obj, VisualizationOptions options)
        {
            return VisualizeObject(obj, this._vis, options);
        }

        public NavigationTarget ParseQueryString(string queryString)
        {
            if (queryString.Length <= 2) return null;

            if (queryString[0] == '?')
            {
                queryString = queryString.Substring(1);
            }

            string[] arr = queryString.Split('&');
            string assemblyName = string.Empty;
            string tokenStr = string.Empty;
            string instructionStr = string.Empty;
            int token;

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].Length <= 2) continue;

                int index = arr[i].IndexOf('=');

                if (index < 0 || index >= arr[i].Length - 1) continue;

                string name = WebUtility.UrlDecode(arr[i].Substring(0, index));
                string val = WebUtility.UrlDecode(arr[i].Substring(index + 1));

                if (Utils.StringEquals(name, "assembly")) assemblyName = val;
                else if (Utils.StringEquals(name, "token")) tokenStr = val;
                else if (Utils.StringEquals(name, "instruction")) instructionStr = val;
            }

            if (instructionStr != string.Empty) //instruction navigation
            {
                uint num;

                if (uint.TryParse(instructionStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out num))
                {
                    NavigationTarget target = new NavigationTarget();
                    target.Kind = NavigationTargetKind.Instruction;
                    target.InstructionNumber = num;
                    return target;
                }
            }

            if (string.IsNullOrEmpty(assemblyName) || string.IsNullOrEmpty(tokenStr))
            {
                return null;
            }

            if (!int.TryParse(tokenStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out token))
            {
                return null;
            }

            Assembly ass = this.ResolveAssembly(assemblyName);

            if (ass == null) return null;

            return new NavigationTarget(ResolveMember(ass, token));
        }

        protected override void RenderPage(string url, HttpListenerRequest request, HttpListenerResponse response)
        {
            string content;

            if (url.StartsWith(this._urlPrefix + "render.html"))
            {
                try
                {
                    string assemblyName = request.QueryString["assembly"];
                    Assembly ass = this.ResolveAssembly(assemblyName);

                    if (ass == null)
                    {
                        SendErrorResponse(response, 404, "Not found");
                        return;
                    }

                    string tokenStr = request.QueryString["token"];
                    int token;

                    if (string.IsNullOrEmpty(tokenStr))
                    {
                        //render assembly manifest
                        IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);
                        content = this._vis.RenderToString(nodes);
                        
                        //send response
                        content = PrepareContent(content);
                        SendHtmlResponse(response, content);
                        this.AddToCache(url, content);
                        return;
                    }

                    if (!int.TryParse(tokenStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out token))
                    {
                        SendErrorResponse(response, 404, "Not found");
                        return;
                    }

                    MemberInfo member = ResolveMember(ass, token);
                    content = this.Visualize(member, new VisualizationOptions());
                    SendHtmlResponse(response, content);
                    this.AddToCache(url, content);
                }
                catch (Exception ex)
                {
                    string error = "<pre>" + WebUtility.HtmlEncode(ex.ToString()) + "</pre>";
                    SendHtmlResponse(response, error);
                }
            }
            else
            {
                SendErrorResponse(response, 404, "Not found");
            }
        }
    }

    class NavigationTarget
    {
        public NavigationTarget() { }

        public NavigationTarget(MemberInfo member)
        {
            this.Member = member;
            this.Kind = NavigationTargetKind.Member;
        }

        public NavigationTargetKind Kind { get; set; }

        public MemberInfo Member { get; set; }

        public uint InstructionNumber { get; set; }
    }

    enum NavigationTargetKind
    {
        Member = 1, Instruction = 2
    }
}
