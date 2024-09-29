/* CIL Tools 
 * Copyright (c) 2024,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using CilTools.Syntax;
using CilTools.Visualization;
using CilView.Common;
using Html.Forms;
using Html.Forms.Controls;
using CVC = CilView.Common;

namespace CilView.UI
{
    class IndexPage : Page
    {
        AssemblySource source;
        AssemblyUrlProvider urlProvider;
        HtmlVisualizer vis = new HtmlVisualizer();
        ComboBox cbType;
        NavigationPanel panelMethods;

        public IndexPage()
        {
            this.FileName = "index.html";
            this.Html = ReadFromResource(this.FileName);

            this.cbType = new ComboBox("cbType");
            this.AddControl(this.cbType);

            this.panelMethods = new NavigationPanel("panelMethods");
            this.panelMethods.IsVertical = true;
            this.AddControl(this.panelMethods);
        }
        
        protected override void OnLoad(LoadEventArgs args)
        {
            if (this.source == null || this.source.Assemblies.Count == 0) return;

            Assembly ass = this.source.Assemblies[0];
            int token;

            if (args.IsInitialLoad && args.HasQueryParam("method"))
            {
                // View method
                string method = args.GetQueryParam("method");
                
                if (!int.TryParse(method, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out token))
                {
                    args.CustomResponse = ResponseData.FromContent("Invalid method token", ContentTypes.PlainText);
                    return;
                }

                MethodBase mb = ResolveMethod(ass, token);

                if (mb == null)
                {
                    args.CustomResponse = ResponseData.FromContent("Method not found!", ContentTypes.PlainText);
                    return;
                }

                CilGraph gr = CilGraph.Create(mb);
                MethodDefSyntax mds = gr.ToSyntaxTree(new DisassemblerParams());
                string rendered = this.vis.RenderToString(mds.GetChildNodes());
                this.SetField("result-html", rendered);
                return;
            }
            else if (args.IsInitialLoad && args.HasQueryParam("type"))
            {
                // View type
                string type = args.GetQueryParam("type");

                if (!int.TryParse(type, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out token))
                {
                    args.CustomResponse = ResponseData.FromContent("Invalid type token", ContentTypes.PlainText);
                    return;
                }

                Type t = ResolveMember(ass, token) as Type;

                if (t == null)
                {
                    args.CustomResponse = ResponseData.FromContent("Type not found!", ContentTypes.PlainText);
                    return;
                }

                this.ViewTypeImpl(t);
                return;
            }
            
            // View assembly manifest
            IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);
            string html = this.vis.RenderToString(nodes);
            this.SetField("result-html", html);
        }

        static MethodBase ResolveMethod(Assembly ass, int metadataToken)
        {
            return ResolveMember(ass, metadataToken) as MethodBase;
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

        internal void LoadSource(AssemblySource newval)
        {
            AssemblySource.TypeCacheClear();
            this.cbType.SelectedIndex = -1;

            if (this.source != null)
            {
                this.source.Dispose();
                this.source = null;
                this.cbType.ItemsSource = null;
                this.cbType.RemoveAllItems();
            }

            this.source = newval;

            if (this.source.Assemblies.Count == 0) return;

            Assembly ass = this.source.Assemblies[0];
            this.source.Types.Clear();
            this.source.Methods.Clear();
            this.source.Types = AssemblySource.LoadTypes(ass);
            this.cbType.ItemsSource = this.source.Types;

            //configure HTML rendering
            this.urlProvider = new AssemblyUrlProvider(ass);
            this.vis.RemoveAllProviders();
            this.vis.AddUrlProvider(this.urlProvider);
        }

        void ViewTypeImpl(Type t)
        {
            // Render type definition IL
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, false, new DisassemblerParams());
            string rendered = this.vis.RenderToString(nodes);
            this.SetField("result-html", rendered);

            // Load methods list
            this.panelMethods.Clear();
            MemberInfo[] members = t.GetMethods(Utils.AllMembers | BindingFlags.DeclaredOnly);
            List<MethodBase> methods = new List<MethodBase>(members.Length);

            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] is MethodBase) methods.Add((MethodBase)members[i]);
            }

            methods.Sort((x, y) =>
            {
                string s1 = string.Empty, s2 = string.Empty;

                try
                {
                    s1 = AssemblySource.MethodToString(x);
                    s2 = AssemblySource.MethodToString(y);
                }
                catch (TypeLoadException) { }
                catch (FileNotFoundException) { }

                return string.Compare(s1, s2, StringComparison.InvariantCulture);
            });

            for (int i = 0; i < methods.Count; i++)
            {
                this.panelMethods.AddLink(AssemblySource.MethodToString(methods[i]),
                    this.urlProvider.GetMemberUrl(methods[i]));
            }

            this.source.Methods = new ObservableCollection<MethodBase>(methods);
        }

        public void ViewType()
        {
            if (this.cbType.SelectedIndex < 0)
            {
                this.SetField("result-html", "Error: No type selected!");
                return;
            }

            Type t = this.source.Types[this.cbType.SelectedIndex];
            this.ViewTypeImpl(t);
        }

        public void OnSearch()
        {
            //validation
            if (this.source == null) return;

            string text = this.GetFieldAs<string>("searchquery");

            if (text == null) text = string.Empty;
            else text = text.Trim();

            if (text.Length == 0)
            {
                string errmes;

                if (this.source.Methods != null && this.source.Methods.Count > 0)
                {
                    errmes = "Enter the method or type name fragment to search";
                }
                else
                {
                    errmes = "Enter the type name fragment to search";
                }

                this.RegisterStartupScript("alert('" + errmes + "');");
                return;
            }

            //actual search
            StringBuilder sb = new StringBuilder();
            CVC.HtmlBuilder html = new CVC.HtmlBuilder(sb);
            html.WriteElement("h2", "Search results");

            try
            {
                IEnumerable<SearchResult> searcher = this.source.Search(text);
                int i = 0;

                foreach (SearchResult item in searcher)
                {
                    //print result hyperlink
                    MemberInfo target = item.Value as MemberInfo;

                    if (target != null)
                    {
                        html.WriteOpeningTag("p");
                        html.WriteElement("a", item.Name,
                            CVC.HtmlBuilder.OneAttribute("href", this.urlProvider.GetMemberUrl(target)));
                        html.WriteClosingTag("p");
                    }
                    else
                    {
                        html.WriteElement("p", item.Name);
                    }

                    i++;

                    if (i >= 50) break; //limit results count
                }

                if (i == 0)
                {
                    html.WriteElement("p", "No items matching the query \"" + text + "\" were found");
                }
            }
            catch (Exception ex)
            {
                html.WriteElement("p", ex.ToString());
            }

            //show search results
            this.SetField("result-html", sb.ToString());
        }
    }
}
