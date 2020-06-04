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

namespace CilView
{
    static class CilVisualization
    {
        internal static string MethodToString(MethodBase m)
        {
            if (m is CilTools.Runtime.ClrMethodInfo)
            {
                string sig = ((CilTools.Runtime.ClrMethodInfo)m).InnerMethod.GetFullSignature();
                string name = m.Name;
                int param_start = sig.IndexOf('(');
                int param_end = sig.IndexOf(')')+1;
                string parstr = sig.Substring(param_start, param_end - param_start);
                return name + parstr;
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
            
            for (int i=0;i<methods.Count;i++ )
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
                        
            return tlv;
        }

        static void VisualizeNode(SyntaxNode node, Paragraph target, RoutedEventHandler navigation)
        {
            Run r;
            StringBuilder sb = new StringBuilder(500);
            StringWriter wr = new StringWriter(sb);

            if (node is InstructionSyntax)
            {
                InstructionSyntax ins = (InstructionSyntax)node;
                
                r = new Run();
                wr.Write(ins.LeadingWhitespace);
                ins.WriteLabel(wr);
                ins.WriteOperation(wr);
                wr.Flush();
                r.Text = sb.ToString();
                target.Inlines.Add(r);

                sb.Clear();
                ins.WriteOperand(wr);
                wr.Flush();
                r = new Run();
                r.Text = sb.ToString();

                if (ins.Instruction.ReferencedMember is MethodBase)
                {
                    //if operand is method, enable navigation functionality
                    Hyperlink lnk = new Hyperlink(r);
                    lnk.Tag = (MethodBase)ins.Instruction.ReferencedMember;
                    lnk.Click += navigation;
                    target.Inlines.Add(lnk);
                }
                else if (ins.Instruction.ReferencedString != null)
                {
                    //render string literal
                    r.Foreground = Brushes.Red;
                    target.Inlines.Add(r);
                }
                else if (sb.Length > 0)
                {
                    //render regular operand
                    r.Foreground = Brushes.CornflowerBlue;
                    target.Inlines.Add(r);
                }

                target.Inlines.Add(new LineBreak());
            }
            else if (node is KeywordSyntax)
            {
                r = new Run();
                r.Foreground = Brushes.Blue;
                r.Text = node.ToString();
                target.Inlines.Add(r);
            }
            else if (node is IdentifierSyntax)
            {
                IdentifierSyntax id = (IdentifierSyntax)node;
                r = new Run();
                if(id.IsMemberName) r.Foreground = Brushes.CornflowerBlue;
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
                        VisualizeNode(children[i], target, navigation);
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

        public static UIElement VisualizeGraph(CilGraph gr, RoutedEventHandler navigation)
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

            for (int i = 0; i < tree.Length; i++) VisualizeNode(tree[i], par, navigation);

            fd.Blocks.Add(par);
            scroll.Document = fd;
            return scroll;
        }
    }
}
