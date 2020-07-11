/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.IO;
using System.Reflection;
using CilTools.Runtime;
using CilTools.BytecodeAnalysis;

namespace CilView
{
    /// <summary>
    /// ThreadsWindow codebehind
    /// </summary>
    public partial class ThreadsWindow : Window
    {
        ClrThreadInfo[] threads;
        TextListViewer tlv;
        MethodBase current_method = null;

        public ThreadsWindow(ClrThreadInfo[] th)
        {
            InitializeComponent();
            this.threads = th;
            cbThread.ItemsSource = threads;
        }

        private void cbThread_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ClrThreadInfo item = (ClrThreadInfo)cbThread.SelectedItem;

                if (item == null) return;

                StringBuilder sb = new StringBuilder(500);
                StringWriter sw = new StringWriter(sb);

                foreach (ClrStackFrameInfo frame in item.StackTrace)
                {
                    sw.WriteLine(frame.ToString());
                }

                this.tlv = CilVisualization.VisualizeStackTrace(item, navigation, null);
                cStackTrace.Child = this.tlv;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.GetType().ToString() + ": " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        MethodBase ExtractMethod(FrameworkContentElement fce)
        {
            MethodBase mb;
            ClrStackFrameInfo frame = fce.Tag as ClrStackFrameInfo;

            if (frame != null)
            {
                mb = frame.Method;
            }
            else
            {
                mb = fce.Tag as MethodBase;
            }

            return mb;
        }

        void NavigateToMethod(MethodBase mb, int start, int end)
        {
            if (mb == null) return;

            CilGraph gr = null;
            gr = CilGraph.Create(mb);

            gridMethod.Children.Clear();
            gridMethod.Children.Add(CilVisualization.VisualizeGraph(gr, navigation, start, end));
            this.current_method = mb;

            //expand content pane
            columnContent.Width = new GridLength(grid.ActualWidth * 0.7);

            //select method in method list
            this.tlv.SelectedItem = null;

            for (int i = 0; i < this.tlv.ItemCount; i++)
            {
                Inline item = this.tlv.GetItem(i);
                MethodBase itemmethod = this.ExtractMethod(item);

                if (itemmethod == null) continue;

                if (ReferenceEquals(itemmethod, mb))
                {
                    this.tlv.SelectedIndex = i;
                    break;
                }
            }
        }

        private void navigation(object sender, RoutedEventArgs e)
        {
            try
            {
                FrameworkContentElement elem = (FrameworkContentElement)sender;
                MethodBase mb;
                ClrStackFrameInfo frame = elem.Tag as ClrStackFrameInfo;

                if (frame != null)
                {
                    mb = frame.Method;
                    int start = frame.ILOffset;
                    int end = frame.ILOffsetEnd;

                    this.NavigateToMethod(mb, start, end);
                }
                else
                {
                    mb = elem.Tag as MethodBase;

                    this.NavigateToMethod(mb, -1, Int32.MaxValue);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.GetType().ToString()+": "+ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
