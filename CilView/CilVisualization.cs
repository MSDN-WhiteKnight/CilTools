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

        public static UIElement VisualizeGraph(CilGraph gr, RoutedEventHandler navigation)
        {
            FlowDocumentScrollViewer scroll = new FlowDocumentScrollViewer();
            scroll.HorizontalAlignment = HorizontalAlignment.Stretch;
            scroll.VerticalAlignment = VerticalAlignment.Stretch;            
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

            FlowDocument fd = new FlowDocument();
            fd.TextAlignment = TextAlignment.Left;
            fd.FontFamily = new FontFamily("Courier New");

            Run r;
            StringBuilder sb = new StringBuilder(500);
            StringWriter wr = new StringWriter(sb);

            foreach(SyntaxNode elem in gr.ToSyntax())
            {
                if (elem is InstructionSyntax)
                {
                    InstructionSyntax ins = (InstructionSyntax)elem;
                    Paragraph line = new Paragraph();
                    line.Margin = new Thickness(0);

                    r = new Run();
                    wr.Write(ins.LeadingTrivia);
                    ins.WriteLabel(wr);
                    ins.WriteOperation(wr);
                    wr.Flush();
                    r.Text = sb.ToString();
                    line.Inlines.Add(r);

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
                        line.Inlines.Add(lnk);
                    }
                    else if (ins.Instruction.ReferencedString != null)
                    {
                        //render string literal
                        r.Foreground = Brushes.Red;
                        line.Inlines.Add(r);
                    }
                    else if (sb.Length > 0)
                    {
                        //render regular operand
                        r.Foreground = Brushes.CornflowerBlue;
                        line.Inlines.Add(r);
                    }
                    
                    fd.Blocks.Add(line);
                    sb.Clear();
                }
                else if (elem is DirectiveSyntax)
                {
                    DirectiveSyntax dir = (DirectiveSyntax)elem;
                    Paragraph line = new Paragraph();
                    line.Margin = new Thickness(0);

                    r = new Run();
                    wr.Write(dir.LeadingTrivia);
                    wr.Write('.');
                    wr.Flush();
                    r.Text = sb.ToString();
                    line.Inlines.Add(r);
                    sb.Clear();

                    r = new Run();
                    r.Text = dir.Name+" ";
                    r.Foreground = Brushes.Magenta;
                    line.Inlines.Add(r);

                    if (dir.InnerElementsCount > 1)
                    {
                        foreach (SyntaxNode subelem in dir.InnerSyntax)
                        {
                            r = new Run();
                            r.Text = subelem.ToString();
                            if (subelem is KeywordSyntax) r.Foreground = Brushes.Blue;
                            else if (subelem is TypeRefSyntax) r.Foreground = Brushes.CornflowerBlue;
                            line.Inlines.Add(r);
                        }
                    }
                    else
                    {
                        r = new Run();
                        r.Text = dir.Content;
                        line.Inlines.Add(r);
                    }

                    fd.Blocks.Add(line);
                }
                else if (elem is BlockStartSyntax)
                {
                    BlockStartSyntax bss = (BlockStartSyntax)elem;
                    Paragraph line = new Paragraph();
                    line.Margin = new Thickness(0);

                    r = new Run();
                    bss.WriteHeader(wr);
                    wr.Flush();
                    r.Text = sb.ToString();
                    r.Foreground = Brushes.MediumAquamarine;
                    line.Inlines.Add(r);
                    sb.Clear();

                    r = new Run();
                    r.Text = "{";
                    line.Inlines.Add(r);

                    fd.Blocks.Add(line);
                }
                else if (elem is CommentSyntax)
                {
                    CommentSyntax c = (CommentSyntax)elem;
                    r = new Run();
                    c.ToText(wr);
                    wr.Flush();
                    r.Text = sb.ToString();
                    r.Foreground = Brushes.Green;
                    fd.Blocks.Add(new Paragraph(r) { Margin = new Thickness(0) });
                    sb.Clear();
                }
                else
                {
                    r = new Run();
                    r.Text = elem.ToString();
                    fd.Blocks.Add(new Paragraph(r) { Margin = new Thickness(0) });
                }
            }

            scroll.Document = fd;
            return scroll;
        }
    }
}
