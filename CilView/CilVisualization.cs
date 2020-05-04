/* CIL Tools 
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
        public static UIElement VisualizeGraph(CilGraph gr, MouseButtonEventHandler navigation)
        {
            ScrollViewer scroll = new ScrollViewer();
            scroll.HorizontalAlignment = HorizontalAlignment.Stretch;
            scroll.VerticalAlignment = VerticalAlignment.Stretch;            
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            scroll.FontSize = 14.0;
            scroll.FontFamily = new FontFamily("Courier New");

            StackPanel pan = new StackPanel();
            pan.Orientation = Orientation.Vertical;

            TextBlock tbl;
            StringBuilder sb = new StringBuilder(500);
            StringWriter wr = new StringWriter(sb);

            foreach(SyntaxElement elem in gr.ToSyntax())
            {
                if (elem is InstructionSyntax)
                {
                    InstructionSyntax ins = (InstructionSyntax)elem;
                    StackPanel line = new StackPanel();
                    line.Orientation = Orientation.Horizontal;

                    /*TextBox tb = new TextBox();
                    tb.Width = Double.NaN;
                    tb.Height = Double.NaN;
                    tb.BorderThickness = new Thickness(0);*/

                    tbl = new TextBlock();
                    ins.WriteLead(wr);
                    ins.WriteLabel(wr);
                    ins.WriteOperation(wr);
                    wr.Flush();
                    tbl.Text = sb.ToString();
                    line.Children.Add(tbl);

                    sb.Clear();
                    ins.WriteOperand(wr);
                    wr.Flush();
                    tbl = new TextBlock();
                    tbl.Text = sb.ToString();
                    //tbl.VerticalAlignment = VerticalAlignment.Stretch;

                    if (ins.Instruction.ReferencedMember is MethodBase)
                    {
                        //if operand is method, enable navigation functionality
                        tbl.TextDecorations.Add(TextDecorations.Underline);
                        tbl.Foreground = Brushes.Blue;

                        tbl.MouseDown += navigation;
                        tbl.Cursor = Cursors.Hand;
                        tbl.Tag = (MethodBase)ins.Instruction.ReferencedMember;
                        line.Children.Add(tbl);
                    }
                    else if (ins.Instruction.ReferencedString != null)
                    {
                        //render string literal
                        tbl.Foreground = Brushes.Red;
                        line.Children.Add(tbl);
                    }
                    else if (sb.Length > 0)
                    {
                        //render regular operand
                        tbl.Foreground = Brushes.CornflowerBlue;
                        line.Children.Add(tbl);
                    }
                    
                    pan.Children.Add(line);
                    sb.Clear();
                }
                else if (elem is DirectiveSyntax)
                {
                    DirectiveSyntax dir = (DirectiveSyntax)elem;
                    StackPanel line = new StackPanel();
                    line.Orientation = Orientation.Horizontal;

                    tbl = new TextBlock();
                    dir.WriteLead(wr);
                    wr.Write('.');
                    wr.Flush();
                    tbl.Text = sb.ToString();
                    line.Children.Add(tbl);
                    sb.Clear();

                    tbl = new TextBlock();
                    tbl.Text = dir.Name+" ";
                    tbl.Foreground = Brushes.Magenta;
                    line.Children.Add(tbl);

                    tbl = new TextBlock();
                    tbl.Text = dir.Content;
                    tbl.Foreground = Brushes.MediumAquamarine;
                    line.Children.Add(tbl);

                    pan.Children.Add(line);
                }
                else if (elem is BlockStartSyntax)
                {
                    BlockStartSyntax bss = (BlockStartSyntax)elem;
                    StackPanel line = new StackPanel();
                    line.Orientation = Orientation.Horizontal;

                    tbl = new TextBlock();
                    bss.WriteHeader(wr);
                    wr.Flush();
                    tbl.Text = sb.ToString();
                    tbl.Foreground = Brushes.MediumAquamarine;
                    line.Children.Add(tbl);
                    sb.Clear();

                    tbl = new TextBlock();
                    tbl.Text = "{";
                    line.Children.Add(tbl);

                    pan.Children.Add(line);
                }
                else if (elem is CommentSyntax)
                {
                    CommentSyntax c = (CommentSyntax)elem;
                    tbl = new TextBlock();
                    c.ToText(wr);
                    wr.Flush();
                    tbl.Text = sb.ToString();
                    tbl.Foreground = Brushes.Green;
                    pan.Children.Add(tbl);
                    sb.Clear();
                }
                else
                {
                    tbl = new TextBlock();
                    tbl.Text = elem.ToString();
                    pan.Children.Add(tbl);
                }
            }

            //signature
            /*TextBlock tbl = new TextBlock();
            tbl.Text = gr.ToString();
            tbl.Foreground = Brushes.Magenta;
            pan.Children.Add(tbl);
            tbl = new TextBlock();
            tbl.Text = "{";
            pan.Children.Add(tbl);*/                        

            //defaults
            /*gr.PrintDefaults(wr);
            wr.Flush();
            if (sb.Length > 0)
            {
                tbl = new TextBlock();
                tbl.Text = sb.ToString();
                tbl.Foreground = Brushes.DarkGray;
                pan.Children.Add(tbl);
                sb.Clear();
            }*/

            //attributes
            /*gr.PrintAttributes(wr);
            wr.Flush();
            tbl = new TextBlock();
            tbl.Text = sb.ToString();
            tbl.Foreground = Brushes.DarkGray;
            pan.Children.Add(tbl);
            sb.Clear();*/

            //header
            /*gr.PrintHeader(wr);
            wr.WriteLine();
            wr.Flush();
            tbl = new TextBlock();
            tbl.Text = sb.ToString();
            tbl.Foreground = Brushes.DarkGray;
            pan.Children.Add(tbl);
            sb.Clear();*/

            //instructions
            /*foreach (var ins in gr.GetInstructions())
            {
                StackPanel line = new StackPanel();
                line.Orientation = Orientation.Horizontal;
                tbl = new TextBlock();
                tbl.Text = ins.OpCode.Name+" ";
                line.Children.Add(tbl);

                ins.OperandToString(wr);
                wr.Flush();
                tbl = new TextBlock();
                tbl.Text = sb.ToString();
                tbl.VerticalAlignment = VerticalAlignment.Stretch;

                if (ins.ReferencedMember is MethodBase)
                {
                    //if operand is method, enable navigation functionality
                    tbl.TextDecorations.Add(TextDecorations.Underline);
                    tbl.Foreground = Brushes.Blue;
                    line.Children.Add(tbl);
                    tbl.MouseDown += navigation;
                    tbl.Cursor = Cursors.Hand;
                    tbl.Tag = (MethodBase)ins.ReferencedMember;
                }
                else if (sb.Length > 0) 
                {
                    //render regular operand
                    tbl.Foreground = Brushes.CornflowerBlue;
                    line.Children.Add(tbl);
                }

                pan.Children.Add(line);
                sb.Clear();
            }

            tbl = new TextBlock();
            tbl.Text = "}";
            pan.Children.Add(tbl);*/

            scroll.Content = pan;
            return scroll;
        }
    }
}
