/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
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
using System.Windows.Input;
using System.Windows.Media;
using CilTools.BytecodeAnalysis;
using CilTools.Syntax;
using CilTools.Runtime;
using CilView.Symbols;
using CilView.UI.Controls;
using CilView.UI.Dialogs;

namespace CilView
{
    static class CilVisualization
    {
        //Default WPF hyperlink color
        //static readonly SolidColorBrush HyperlinkBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x66, 0xCC));

        //Default in Visual Studio for types
        static readonly SolidColorBrush IdentifierBrush = new SolidColorBrush(Color.FromArgb(0xFF, 43, 145, 175));

        static ContextMenu s_instructionMenu;
        static FrameworkContentElement s_instructionMenuTarget;

        static ContextMenu GetInstructionMenu()
        {
            if (s_instructionMenu != null) return s_instructionMenu;

            ContextMenu menu = new ContextMenu();
            MenuItem mi;
            mi = new MenuItem();
            mi.Header = "Show source";
            mi.Click += Mi_ShowSource_Click;
            menu.Items.Add(mi);
            mi = new MenuItem();
            mi.Header = "Show source (method)";
            mi.Click += Mi_ShowSource_Method_Click;
            menu.Items.Add(mi);
            s_instructionMenu = menu;
            return menu;
        }

        static void ShowSource(CilInstruction instr, bool wholeMethod)
        {
            try
            {
                SourceInfo srcinfo;

                if (wholeMethod)
                {
                    srcinfo = PdbUtils.GetSourceFromPdb(instr.Method,
                        0, uint.MaxValue, true);
                }
                else
                {
                    srcinfo = PdbUtils.GetSourceFromPdb(instr.Method,
                        instr.ByteOffset, instr.ByteOffset, false);
                }

                string src = srcinfo.SourceCode;
                
                if (string.IsNullOrWhiteSpace(src))
                {
                    MessageBox.Show("Failed to get source code", "Error");
                    return;
                }
                
                if (wholeMethod)
                {
                    //build display string
                    StringBuilder sb = new StringBuilder(src.Length * 2);
                    sb.AppendLine("Method: ");
                    sb.AppendLine(MethodToString(srcinfo.Method));
                    sb.AppendLine();

                    sb.AppendLine("Source code: ");
                    sb.AppendFormat("({0}, ", srcinfo.SourceFile);
                    sb.AppendFormat("lines {0}-{1})", srcinfo.LineStart, srcinfo.LineEnd);
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine(src);
                    sb.AppendLine();

                    //show source code
                    TextViewWindow wnd = new TextViewWindow();
                    wnd.Title = "Source code";
                    wnd.Text = sb.ToString();
                    wnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    wnd.ShowDialog();
                }
                else
                {
                    SourceViewWindow srcwnd = new SourceViewWindow(srcinfo);
                    srcwnd.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                if (ex is NotSupportedException || ex is SymbolsException)
                {
                    //don't pollute logs with expected errors
                    MessageBox.Show(ex.Message, "Error");
                }
                else
                {
                    ErrorHandler.Current.Error(ex);
                }
            }
        }

        private static void Mi_ShowSource_Click(object sender, RoutedEventArgs e)
        {
            if (s_instructionMenuTarget == null) return;

            //show source code corresponding to instruction
            InstructionSyntax syntax = s_instructionMenuTarget.Tag as InstructionSyntax;
            if(syntax==null) return;
            if(syntax.Instruction==null) return;

            CilInstruction instr = syntax.Instruction;
                        
            ShowSource(instr, false);
        }

        private static void Mi_ShowSource_Method_Click(object sender, RoutedEventArgs e)
        {
            if (s_instructionMenuTarget == null) return;
            
            //get instruction that user right-clicked on
            InstructionSyntax syntax = s_instructionMenuTarget.Tag as InstructionSyntax;
            if (syntax == null) return;
            if (syntax.Instruction == null) return;

            CilInstruction instr = syntax.Instruction;

            //show source code of method
            ShowSource(instr, true);
        }

        private static void R_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            //saves the content element that user right-clicked on, so we can figure out later
            //which instruction this context menu points to
            s_instructionMenuTarget = sender as FrameworkContentElement;
        }

        internal static string MethodToString(MethodBase m)
        {
            if (m is CilTools.Runtime.ClrMethodInfo)
            {
                string sig = ((CilTools.Runtime.ClrMethodInfo)m).InnerMethod.GetFullSignature();
                string name = m.Name;

                int index = sig.IndexOf(name);
                if (index < 0) index = 0;

                return sig.Substring(index);
            }

            StringBuilder sb = new StringBuilder();
            Type t = m.DeclaringType;
            ParameterInfo[] pars = m.GetParameters();

            MethodInfo mi = m as MethodInfo;

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
                        r.ContextMenu = GetInstructionMenu();
                        r.ContextMenuOpening += R_ContextMenuOpening;
                        r.Tag = par;
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

                if (m != null && !(node.Parent is DirectiveSyntax))
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
                    if (ctx.navigation != null) lnk.Click += ctx.navigation;
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
            public bool ScrollCallbackApplied = false;
        }

        public static UIElement VisualizeGraph(
            CilGraph gr, RoutedEventHandler navigation,int highlight_start=-1,int highlight_end=Int32.MaxValue
            )
        {
            FlowDocumentScrollViewer scroll = new FlowDocumentScrollViewer();
            scroll.HorizontalAlignment = HorizontalAlignment.Stretch;
            scroll.VerticalAlignment = VerticalAlignment.Stretch;
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

            FlowDocument fd = new FlowDocument();
            fd.TextAlignment = TextAlignment.Left;
            fd.FontFamily = new FontFamily("Courier New");
            
            SyntaxNode[] tree = gr.ToSyntaxTree().GetChildNodes();
            Paragraph par = new Paragraph();

            VisualizeGraphContext ctx = new VisualizeGraphContext();
            ctx.navigation = navigation;
            ctx.highlight_start = highlight_start;
            ctx.highlight_end = highlight_end;

            for (int i = 0; i < tree.Length; i++) VisualizeNode(tree[i], par, ctx);

            fd.Blocks.Add(par);
            scroll.Document = fd;
            return scroll;
        }

        public static UIElement VisualizeType(Type t, RoutedEventHandler navigation,out string plaintext)
        {
            FlowDocumentScrollViewer scroll = new FlowDocumentScrollViewer();
            scroll.HorizontalAlignment = HorizontalAlignment.Stretch;
            scroll.VerticalAlignment = VerticalAlignment.Stretch;
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

            FlowDocument fd = new FlowDocument();
            fd.TextAlignment = TextAlignment.Left;
            fd.FontFamily = new FontFamily("Courier New");

            IEnumerable<SyntaxNode> tree = SyntaxNode.GetTypeDefSyntax(t);
            StringBuilder sb = new StringBuilder(500);
            StringWriter wr = new StringWriter(sb);
            Paragraph par = new Paragraph();

            VisualizeGraphContext ctx = new VisualizeGraphContext();
            ctx.navigation = navigation;

            foreach (SyntaxNode node in tree) 
            {
                VisualizeNode(node, par, ctx);
                node.ToText(wr);
            }

            fd.Blocks.Add(par);
            scroll.Document = fd;
            plaintext = sb.ToString();
            return scroll;
        }
    }
}
