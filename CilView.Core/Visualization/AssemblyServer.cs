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

namespace CilView.Visualization
{
    /// <summary>
    /// Provides a server that dynamically generates HTML for a disassembled IL and returns it via HTTP
    /// </summary>
    public sealed class AssemblyServer : ServerBase
    {
        Assembly _ass;        
        CilVisualizer _vis;
        
        public AssemblyServer(Assembly ass, string urlHost, string urlPrefix) : base(urlHost, urlPrefix)
        {
            this._ass = ass;
            this._vis = new CilVisualizer();
            AssemblyUrlProvider provider = new AssemblyUrlProvider(this._ass);
            this._vis.AddUrlProvider(provider);
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

        protected override void RenderPage(string url, HttpListenerRequest request, HttpListenerResponse response)
        {
            string content;

            if (url.StartsWith(this._urlPrefix + "render.html"))
            {
                string tokenStr = request.QueryString["token"];
                int token;

                if (string.IsNullOrEmpty(tokenStr))
                {
                    content = this.PrepareContent(this._vis.RenderAssemblyManifest(this._ass));
                    SendHtmlResponse(response, content);
                    this.AddToCache(url, content);
                    return;
                }

                if (!int.TryParse(tokenStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out token))
                {
                    SendErrorResponse(response, 404, "Not found");
                    return;
                }

                MemberInfo member = ResolveMember(this._ass, token);

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
