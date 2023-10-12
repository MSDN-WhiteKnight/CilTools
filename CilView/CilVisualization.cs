﻿/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using CilTools.BytecodeAnalysis;
using CilTools.Syntax;
using CilTools.Runtime;
using CilView.Core.DocumentModel;
using CilView.SourceCode;
using CilView.UI.Controls;
using CilView.Visualization;

namespace CilView
{
    static class CilVisualization
    {
        //Default WPF hyperlink color
        //static readonly SolidColorBrush HyperlinkBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x66, 0xCC));

        //Default in Visual Studio for types
        internal static readonly SolidColorBrush IdentifierBrush = new SolidColorBrush(Color.FromArgb(0xFF, 43, 145, 175));

        //Parameters that control how disassembled CIL is produced. They are changed from main window UI.
        internal static readonly DisassemblerParams CurrentDisassemblerParams = InitDisassemblerParams();

        internal static VisualizationServer Server;

        static DisassemblerParams InitDisassemblerParams()
        {
            //default disassembler parameters
            DisassemblerParams ret = new DisassemblerParams();
            ret.CodeProvider = PdbCodeProvider.Instance;
            ret.IncludeSourceCode = false;
            return ret;
        }

        internal static string MethodToString(MethodBase m)
        {
            if (m is ClrMethodInfo)
            {
                string sig = ((ClrMethodInfo)m).InnerMethod.GetFullSignature();
                string name = m.Name;

                int index = sig.IndexOf(name);
                if (index < 0) index = 0;

                return sig.Substring(index);
            }

            ParameterInfo[] pars = m.GetParameters();
            StringBuilder sb = new StringBuilder();
            sb.Append(m.Name);

            if (m.IsGenericMethod)
            {
                sb.Append('<');

                Type[] args = m.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                {
                    if (i >= 1) sb.Append(", ");

                    sb.Append(args[i].Name);
                }

                sb.Append('>');
            }

            sb.Append('(');

            for (int i = 0; i < pars.Length; i++)
            {
                if (i >= 1) sb.Append(", ");
                sb.Append(pars[i].ParameterType.Name);
            }

            sb.Append(')');
            return sb.ToString();
        }

        public static TextListViewer VisualizeMethodList(
            ObservableCollection<MethodBase> methods, 
            RoutedEventHandler navigation,
            MethodBase selected=null)
        {
            TextListViewer tlv = new TextListViewer();
            tlv.Document.PagePadding = new Thickness(0,5,5,5);
            
            for (int i=0;i<methods.Count;i++ )
            {
                try
                {
                    MethodBase m = methods[i];
                    Run r = new Run(MethodToString(m));
                    Hyperlink lnk = new Hyperlink(r);
                    lnk.Tag = m;
                    lnk.Click += navigation;
                    tlv.AddItem(lnk);

                    if (selected == null) continue;

                    if (MethodBase.ReferenceEquals(m, selected))
                    {
                        tlv.SelectedIndex = i;
                    }
                }
                catch (TypeLoadException ex) 
                {
                    tlv.AddItem(new Run("[ERROR] TypeLoadException: "+ex.Message));
                }
                catch (FileNotFoundException ex) 
                {
                    tlv.AddItem(new Run("[ERROR] FileNotFoundException: " + ex.Message));
                }
            }
                        
            return tlv;
        }

        public static TextListViewer VisualizeStackTrace(
            ClrThreadInfo thread,
            RoutedEventHandler navigation,
            MethodBase selected = null)
        {
            TextListViewer tlv = new TextListViewer();
            tlv.Document.PagePadding = new Thickness(0, 5, 5, 5);
            int i = 0;

            foreach (ClrStackFrameInfo frame in thread.StackTrace)
            {
                try
                {
                    MethodBase m = frame.Method;
                    
                    if (m != null)
                    {
                        Run r;
                        r = new Run(frame.ToString(false));

                        Hyperlink lnk = new Hyperlink(r);
                        lnk.Tag = frame;
                        lnk.Click += navigation;
                        tlv.AddItem(lnk);

                        if (selected == null) continue;

                        if (MethodBase.ReferenceEquals(m, selected))
                        {
                            tlv.SelectedIndex = i;
                        }
                    }
                    else
                    {
                        tlv.AddItem(new Run(frame.ToString(false)));
                    }
                }
                catch (TypeLoadException ex)
                {
                    tlv.AddItem(new Run("[ERROR] TypeLoadException: " + ex.Message));
                }
                catch (FileNotFoundException ex)
                {
                    tlv.AddItem(new Run("[ERROR] FileNotFoundException: " + ex.Message));
                }

                i++;
            }

            return tlv;
        }

        static void BringToViewOnLoaded(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkContentElement).BringIntoView();
        }

        static void VisualizeNode(
            SyntaxNode node, Paragraph target, VisualizeGraphContext ctx
            )
        {
            Run r;
            StringBuilder sb = new StringBuilder(500);
            StringWriter wr = new StringWriter(sb);

            if (node is MemberRefSyntax)
            {
                MemberRefSyntax mrs = (MemberRefSyntax)node;

                //render member ref
                IEnumerable<SyntaxNode> children = mrs.EnumerateChildNodes();

                foreach (SyntaxNode child in children) VisualizeNode(child, target, ctx);
            }
            else if (node is KeywordSyntax)
            {
                KeywordSyntax ks = (KeywordSyntax)node;
                r = new Run();
                string text=node.ToString();

                if (ks.Kind == KeywordKind.InstructionName) 
                {
                    InstructionSyntax par = ks.Parent as InstructionSyntax;
                    CilInstruction instr = null;

                    if (par != null)
                    {
                        instr = par.Instruction;
                    }

                    if (instr != null)
                    {
                        //attach context menu to instruction opcode
                        if (ctx.ContextMenuEnabled)
                        {
                            r.ContextMenu = InstructionMenu.GetInstructionMenu();
                            r.ContextMenuOpening += InstructionMenu.R_ContextMenuOpening;
                            r.Tag = par;
                        }
                    }
                }

                if (ks.Kind == KeywordKind.InstructionName && ctx.highlight_start >= 0)
                {
                    InstructionSyntax par = ks.Parent as InstructionSyntax;
                    CilInstruction instr=null;

                    if (par != null)
                    {
                        instr = par.Instruction;
                    }

                    if (instr != null)
                    {
                        if (instr.ByteOffset >= ctx.highlight_start && instr.ByteOffset < ctx.highlight_end)
                        {
                            r.Foreground = Brushes.Red;
                            r.FontWeight = FontWeights.Bold;

                            //on first highlighted instruction, apply callback that scrolls FlowDocument to current line
                            if (!ctx.ScrollCallbackApplied)
                            {
                                r.Loaded += BringToViewOnLoaded;
                                ctx.ScrollCallbackApplied = true;
                            }
                        }
                    }
                }
                else if (ks.Kind == KeywordKind.Other)
                    r.Foreground = Brushes.Blue;
                else if (ks.Kind == KeywordKind.DirectiveName)
                    r.Foreground = Brushes.Magenta;
                
                r.Text = text;
                target.Inlines.Add(r);
            }
            else if (node is IdentifierSyntax)
            {
                IdentifierSyntax id = (IdentifierSyntax)node;
                r = new Run();
                MethodBase m = id.TargetMember as MethodBase;

                if (m != null && !(node.Parent is DirectiveSyntax) && ctx.navigation != null)
                {
                    //if target is method and we are not in directive (method sig), 
                    //enable navigation functionality

                    sb.Clear();
                    node.ToText(wr);
                    wr.Flush();
                    r.Text = sb.ToString();

                    Hyperlink lnk = new Hyperlink(r);
                    lnk.TextDecorations = new TextDecorationCollection(); //remove underline
                    lnk.Tag = m;
                    lnk.Click += ctx.navigation;
                    target.Inlines.Add(lnk);
                }
                else if (id.TargetItem is CilInstruction && !id.IsDefinition && ctx.navigation != null)
                {
                    //if target is instruction (label), enable navigation functionality
                    r.Text = node.ToString();
                    Hyperlink lnk = new Hyperlink(r);
                    lnk.TextDecorations = new TextDecorationCollection(); //remove underline
                    lnk.Foreground = Brushes.Black;
                    lnk.Tag = id.TargetItem;
                    lnk.Click += ctx.navigation;
                    target.Inlines.Add(lnk);
                }
                else
                {
                    if (id.IsMemberName) r.Foreground = IdentifierBrush;
                    r.Text = node.ToString();
                    target.Inlines.Add(r);
                }
            }
            else if (node is LiteralSyntax)
            {
                LiteralSyntax lit = (LiteralSyntax)node;
                r = new Run();
                if (lit.Value is string) r.Foreground = Brushes.Red;
                r.Text = node.ToString();
                target.Inlines.Add(r);
            }
            else if (node is CommentSyntax)
            {
                r = new Run();
                r.Foreground = Brushes.Green;
                r.Text = node.ToString();
                target.Inlines.Add(r);
            }
            else
            {
                SyntaxNode[] children = node.GetChildNodes();

                if (children.Length > 0)
                {
                    for (int i = 0; i < children.Length; i++)
                    {
                        VisualizeNode(children[i], target, ctx);
                    }
                }
                else
                {
                    r = new Run();
                    r.Text = node.ToString();
                    target.Inlines.Add(r);
                }
            }
        }

        class VisualizeGraphContext
        {
            public RoutedEventHandler navigation;
            public int highlight_start=-1;
            public int highlight_end = Int32.MaxValue;
            public bool ContextMenuEnabled = true;
            public bool ScrollCallbackApplied = false;
        }
        
        internal static FlowDocument CreateFlowDocument()
        {
            FlowDocument fd = new FlowDocument();
            fd.TextAlignment = TextAlignment.Left;
            fd.FontFamily = new FontFamily("Courier New");
            return fd;
        }

        public static UIElement VisualizeAsHtml(object obj, NavigatingCancelEventHandler navigation)
        {
            string html = Server.Visualize(obj);
            WebBrowser wb = new WebBrowser();
            wb.NavigateToString(html);

            if (navigation != null) wb.Navigating += navigation;

            return wb;
        }

        public static string VisualizeAsText(object obj)
        {
            IEnumerable<SyntaxNode> nodes;

            if (obj is IlasmAssembly)
            {
                //synthesized assembly that contains IL - no need to disassemble
                IlasmAssembly ia = (IlasmAssembly)obj;
                nodes = ia.Syntax.GetChildNodes();
            }
            else if (obj is IlasmType)
            {
                //synthesized type that contains IL - no need to disassemble
                IlasmType dt = (IlasmType)obj;
                nodes = dt.Syntax.GetChildNodes();
            }
            else if (obj is Assembly)
            {
                //assembly manifest
                Assembly ass = (Assembly)obj;
                nodes = Disassembler.GetAssemblyManifestSyntaxNodes(ass);
            }
            else if (obj is Type)
            {
                //type disassembled IL
                Type t = (Type)obj;
                nodes = SyntaxNode.GetTypeDefSyntax(t);
            }
            else if (obj is MethodBase)
            {
                //method disassembled IL
                MethodBase mb = (MethodBase)obj;
                CilGraph gr = CilGraph.Create(mb);
                nodes = gr.ToSyntaxTree(CilVisualization.CurrentDisassemblerParams).GetChildNodes();
            }
            else return string.Empty;

            StringBuilder sb = new StringBuilder(500);
            StringWriter wr = new StringWriter(sb);

            foreach (SyntaxNode node in nodes)
            {
                node.ToText(wr);
            }

            return sb.ToString();
        }
        
        public static FlowDocument VisualizeNodes(IEnumerable<SyntaxNode> nodes)
        {
            FlowDocument fd = CreateFlowDocument();
            Paragraph par = new Paragraph();
            VisualizeGraphContext ctx = new VisualizeGraphContext();
            ctx.ContextMenuEnabled = false;
            ctx.navigation = null;

            foreach (SyntaxNode node in nodes) VisualizeNode(node, par, ctx);

            fd.Blocks.Add(par);
            return fd;
        }
    }
}
