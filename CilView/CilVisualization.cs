﻿/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
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

            foreach(SyntaxElement elem in gr.ToSyntax())
            {
                if (elem is InstructionSyntax)
                {
                    InstructionSyntax ins = (InstructionSyntax)elem;
                    Paragraph line = new Paragraph();
                    line.Margin = new Thickness(0);

                    r = new Run();
                    ins.WriteLead(wr);
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
                    dir.WriteLead(wr);
                    wr.Write('.');
                    wr.Flush();
                    r.Text = sb.ToString();
                    line.Inlines.Add(r);
                    sb.Clear();

                    r = new Run();
                    r.Text = dir.Name+" ";
                    r.Foreground = Brushes.Magenta;
                    line.Inlines.Add(r);

                    r = new Run();
                    r.Text = dir.Content;
                    r.Foreground = Brushes.MediumAquamarine;
                    line.Inlines.Add(r);

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
