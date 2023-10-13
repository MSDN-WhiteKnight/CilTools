/* CIL Tools 
 * Copyright (c) 2022, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CilTools.BytecodeAnalysis;
using CilTools.Visualization;
using CilView.Core.DocumentModel;

namespace CilView.UI.Controls
{
    /// <summary>
    /// CilBrowserPage codebehind
    /// </summary>
    public partial class CilBrowserPage : Page
    {
        MemberInfo member; //assembly member which code is displayed on the page

        public CilBrowserPage(MethodBase m, int start, int end, NavigatingCancelEventHandler navigation)
        {
            InitializeComponent();
            this.member = m;

            CilGraph gr = CilGraph.Create(m);
            string contentText = gr.ToSyntaxTree(CilVisualization.CurrentDisassemblerParams).ToString();

            VisualizationOptions options = new VisualizationOptions();
            options.HighlightingStartOffset = start;
            options.HighlightingEndOffset = end;
            options.SetOption("EnableInstructionDoubleClick", true);
            UIElement elem = CilVisualization.VisualizeAsHtml(m, navigation, options);
            this.tbMainContent.Text = contentText;
            gridContent.Children.Clear();
            gridContent.Children.Add(elem);

            //display method location
            StringBuilder sb = new StringBuilder(1000);
            Type t = m.DeclaringType;
            Assembly ass = null;
            if (t != null) ass = t.Assembly;

            if (ass != null) sb.Append(ass.GetName().Name);
            else sb.Append("???");

            sb.Append(" / ");

            if (t != null) sb.Append(t.FullName);
            else sb.Append("???");

            sb.Append(" / ");
            sb.Append(m.Name);
            this.tbCurrLocation.Text = sb.ToString();
        }

        public CilBrowserPage(Type t, NavigatingCancelEventHandler navigation)
        {
            InitializeComponent();
            this.member = t;

            string plaintext = CilVisualization.VisualizeAsText(t);
            UIElement elem = CilVisualization.VisualizeAsHtml(t, navigation, new VisualizationOptions());
            this.tbMainContent.Text = plaintext;
            gridContent.Children.Clear();
            gridContent.Children.Add(elem);

            //display type location
            StringBuilder sb = new StringBuilder(1000);
            Assembly ass = t.Assembly;

            if (ass != null) sb.Append(ass.GetName().Name);
            else sb.Append("???");

            sb.Append(" / ");
            sb.Append(t.FullName);
            this.tbCurrLocation.Text = sb.ToString();
        }

        public CilBrowserPage(Assembly ass, NavigatingCancelEventHandler navigation)
        {
            InitializeComponent();
            
            string plaintext = CilVisualization.VisualizeAsText(ass);
            UIElement elem = CilVisualization.VisualizeAsHtml(ass, navigation, new VisualizationOptions());
            this.tbMainContent.Text = plaintext;
            gridContent.Children.Clear();
            gridContent.Children.Add(elem);

            if (ass is IlasmAssembly)
            {
                //display title as location for synthesized assembly
                IlasmAssembly ia = (IlasmAssembly)ass;
                this.tbCurrLocation.Text = ia.Title;
            }
            else
            {
                //display assembly name as location for real assembly
                this.tbCurrLocation.Text = ass.GetName().Name;
            }
        }

        public MemberInfo Member { get { return this.member; } }

        public string ContentText
        {
            get { return this.tbMainContent.Text; }
        }
    }
}
