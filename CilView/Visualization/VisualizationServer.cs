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
        CilVisualizer _vis;
        CilViewUrlProvider _provider;

        public VisualizationServer(string urlHost, string urlPrefix) : base(urlHost, urlPrefix)
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

            // CSS only works good starting from Internet Explorer 9
            html.WriteRaw("<meta http-equiv=\"X-UA-Compatible\" content=\"IE=9\"/>");

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
                CilGraph gr = CilGraph.Create(mb);
                MethodDefSyntax mds = gr.ToSyntaxTree(CilVisualization.CurrentDisassemblerParams);
                return this.PrepareContent(this._vis.RenderSyntaxNodes(mds.EnumerateChildNodes()));
            }
            else return string.Empty;
        }

        public MemberInfo ParseQueryString(string queryString)
        {
            if (queryString.Length <= 2) return null;

            if (queryString[0] == '?')
            {
                queryString = queryString.Substring(1);
            }

            string[] arr = queryString.Split('&');
            string assemblyName = string.Empty;
            string tokenStr = string.Empty;
            int token;

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].Length <= 2) continue;

                int index = arr[i].IndexOf('=');

                if (index < 0 || index >= arr[i].Length - 1) continue;

                string name = arr[i].Substring(0, index);
                string val = arr[i].Substring(index + 1);

                if (Utils.StringEquals(name, "assembly")) assemblyName = val;
                else if (Utils.StringEquals(name, "token")) tokenStr = val;
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

            return ResolveMember(ass, token);
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
                    content = this.Visualize(member);
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
}
