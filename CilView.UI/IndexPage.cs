/* CIL Tools 
 * Copyright (c) 2024,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;
using CilTools.Syntax;
using CilTools.Visualization;
using CilView.Common;
using Html.Forms;
using Html.Forms.Controls;

namespace CilView.UI
{
    class IndexPage : Page
    {
        ComboBox cbType;
        AssemblySource source;

        public IndexPage()
        {
            this.FileName = "index.html";
            this.Html = ReadFromResource(this.FileName);

            this.cbType = new ComboBox("cbType");
            this.AddControl(this.cbType);
        }
        
        protected override void OnLoad(LoadEventArgs args)
        {
            if (this.source.Assemblies.Count == 0) return;

            // Initially the assembly manifest is displayed
            Assembly ass = this.source.Assemblies[0];
            IEnumerable<SyntaxNode> nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);
            HtmlVisualizer vis = new HtmlVisualizer();
            string html = vis.RenderToString(nodes);
            this.SetField("result-html", html);
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
        }

        void ViewTypeImpl(Type t)
        {
            // Render type definition IL
            HtmlVisualizer vis = new HtmlVisualizer();
            IEnumerable<SyntaxNode> nodes = SyntaxNode.GetTypeDefSyntax(t, false, new DisassemblerParams());
            string rendered = vis.RenderToString(nodes);
            this.SetField("result-html", rendered);
            
            // Load methods list
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
    }
}
