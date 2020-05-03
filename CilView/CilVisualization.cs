/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using CilTools.BytecodeAnalysis;
using System.Reflection;

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

            //signature
            TextBlock tbl = new TextBlock();
            tbl.Text = gr.ToString();
            tbl.Foreground = Brushes.Magenta;
            pan.Children.Add(tbl);
            tbl = new TextBlock();
            tbl.Text = "{";
            pan.Children.Add(tbl);

            StringBuilder sb = new StringBuilder(500);
            StringWriter wr = new StringWriter(sb);

            //defaults
            gr.PrintDefaults(wr);
            wr.Flush();
            if (sb.Length > 0)
            {
                tbl = new TextBlock();
                tbl.Text = sb.ToString();
                tbl.Foreground = Brushes.DarkGray;
                pan.Children.Add(tbl);
                sb.Clear();
            }

            //attributes
            gr.PrintAttributes(wr);
            wr.Flush();
            tbl = new TextBlock();
            tbl.Text = sb.ToString();
            tbl.Foreground = Brushes.DarkGray;
            pan.Children.Add(tbl);
            sb.Clear();

            //header
            gr.PrintHeader(wr);
            wr.WriteLine();
            wr.Flush();
            tbl = new TextBlock();
            tbl.Text = sb.ToString();
            tbl.Foreground = Brushes.DarkGray;
            pan.Children.Add(tbl);
            sb.Clear();

            //instructions
            foreach (var ins in gr.GetInstructions())
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
            pan.Children.Add(tbl);
            scroll.Content = pan;
            return scroll;
        }
    }
}
