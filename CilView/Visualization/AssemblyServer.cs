/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using CilTools.Reflection;
using CilTools.Visualization;
using CilView.Common;
using CilView.Core.DocumentModel;

namespace CilView.Visualization
{
    /// <summary>
    /// Provides a server that dynamically generates HTML for a disassembled IL and returns it via HTTP
    /// </summary>
    class AssemblyServer : ServerBase
    {
        AssemblySource _src;
        CilVisualizer _vis;
        CilViewUrlProvider _provider;

        public AssemblyServer(string urlHost, string urlPrefix) : base(urlHost, urlPrefix)
        {
            this._vis = new CilVisualizer();
            this._provider = new CilViewUrlProvider();
            this._vis.AddUrlProvider(this._provider);
        }

        public AssemblySource Source
        {
            get { return this._src; }
            set { this._src = value; }
        }

        public UrlProviderBase UrlProvider
        {
            get { return this._provider; }
        }

        public string GetAssemblyUrl(Assembly ass)
        {
            return DefaultUrlHost + DefaultUrlPrefix + "render.html?assembly=" + 
                WebUtility.UrlEncode(Utils.GetAssemblySimpleName(ass));
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
        
        string PrepareContent(string content)
        {
            StringBuilder sb = new StringBuilder();
            HtmlBuilder html = new HtmlBuilder(sb);
            html.WriteOpeningTag("html");
            html.WriteOpeningTag("head");
            html.WriteElement("style", SyntaxVisualizer.GetVisualStyles());
            html.WriteOpeningTag("head");
            html.WriteOpeningTag("body");
            html.WriteRaw(content);
            html.WriteClosingTag("body");
            html.WriteClosingTag("html");
            return sb.ToString();
        }

        public string Visualize(object obj)
        {
            if (obj is IlasmAssembly)
            {
                //synthesized assembly that contains IL - no need to disassemble
                IlasmAssembly ia = (IlasmAssembly)obj;
                string html = this._vis.RenderSyntaxNodes(ia.Syntax.GetChildNodes());
                return this.PrepareContent(html);
            }
            else if (obj is IlasmType)
            {
                //synthesized type that contains IL - no need to disassemble
                IlasmType dt = (IlasmType)obj;
                SyntaxVisualizer visualizer = new SyntaxVisualizer();
                string html = this._vis.RenderSyntaxNodes(dt.Syntax.GetChildNodes());
                return this.PrepareContent(html);
            }
            else if (obj is Assembly)
            {
                //assembly manifest
                Assembly ass = (Assembly)obj;
                return this.PrepareContent(this._vis.RenderAssemblyManifest(ass));
            }
            else if (obj is Type)
            {
                //type disassembled IL
                Type t = (Type)obj;
                return this.PrepareContent(this._vis.RenderType(t, false));
            }
            else if (obj is MethodBase)
            {
                //method disassembled IL
                MethodBase mb = (MethodBase)obj;
                return this.PrepareContent(this._vis.RenderMethod(mb));
            }
            else return string.Empty;
        }

        protected override void RenderPage(string url, HttpListenerRequest request, HttpListenerResponse response)
        {
            string content;

            if (url.StartsWith(this._urlPrefix + "render.html"))
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
                    content = this.PrepareContent(this._vis.RenderAssemblyManifest(ass));
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

                if (member is MethodBase)
                {
                    content = this._vis.RenderMethod((MethodBase)member);
                }
                else if (member is Type)
                {
                    content = this._vis.RenderType((Type)member, false);
                }
                else content = string.Empty;

                content = this.PrepareContent(content);
                SendHtmlResponse(response, content);
                this.AddToCache(url, content);
            }
            else
            {
                SendErrorResponse(response, 404, "Not found");
            }
        }
    }
}
