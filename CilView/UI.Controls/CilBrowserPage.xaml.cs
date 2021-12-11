/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using CilTools.BytecodeAnalysis;

namespace CilView.UI.Controls
{
    /// <summary>
    /// CilBrowserPage codebehind
    /// </summary>
    public partial class CilBrowserPage : Page
    {
        MemberInfo member; //assembly member which code is displayed on the page

        public CilBrowserPage(MethodBase m, int start, int end, RoutedEventHandler navigation)
        {
            InitializeComponent();
            this.member = m;

            CilGraph gr = CilGraph.Create(m);
            string contentText = gr.ToSyntaxTree(CilVisualization.CurrentDisassemblerParams).ToString();
            
            UIElement elem = CilVisualization.VisualizeGraph(gr, navigation, start, end);
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

        public CilBrowserPage(Type t, RoutedEventHandler navigation)
        {
            InitializeComponent();
            this.member = t;

            string plaintext;
            UIElement elem = CilVisualization.VisualizeType(t, navigation, out plaintext);
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

        public MemberInfo Member { get { return this.member; } }

        public string ContentText
        {
            get { return this.tbMainContent.Text; }
        }
    }
}
