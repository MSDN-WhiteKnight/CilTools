/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Reflection;
using System.Collections.ObjectModel;
using System.IO;
using CilTools.BytecodeAnalysis;
using CilTools.Runtime;

namespace CilView.UI.Controls
{
    /// <summary>
    /// CilBrowser control codebehind
    /// </summary>
    public partial class CilBrowser : UserControl
    {
        TextListViewer tlv;
        MethodBase current_method = null;
        Type current_type = null;

        public CilBrowser()
        {
            InitializeComponent();
        }

        public void NavigateToMethod(MethodBase mb)
        {
            this.NavigateToMethod(mb, -1, Int32.MaxValue);
        }

        MethodBase ExtractMethod(FrameworkContentElement fce)
        {
            if (fce.Tag is MethodBase)
            {
                //from method list
                return (MethodBase)fce.Tag;
            }

            //from stack trace view
            MethodBase mb=null;
            ClrStackFrameInfo frame = fce.Tag as ClrStackFrameInfo;

            if (frame != null)
            {
                mb = frame.Method;
            }

            return mb;
        }

        public void NavigateToMethod(MethodBase mb, int start, int end)
        {
            StringBuilder sb = new StringBuilder(1000);
            StringWriter wr = new StringWriter(sb);

            using (wr)
            {
                CilGraph gr = CilGraph.Create(mb);
                gr.Print(wr, true, true, true, true);
                wr.Flush();
                tbMainContent.Text = sb.ToString();

                UIElement elem = CilVisualization.VisualizeGraph(gr, Navigated, start, end);
                gridContent.Children.Clear();
                gridContent.Children.Add(elem);

                this.current_method = mb;
            }

            sb.Clear();

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

            //display method location
            Type t = mb.DeclaringType;
            Assembly ass = null;
            if (t != null) ass = t.Assembly;

            if (ass != null) sb.Append(ass.GetName().Name);
            else sb.Append("???");

            sb.Append(" / ");

            if (t != null) sb.Append(t.FullName);
            else sb.Append("???");

            sb.Append(" / ");
            sb.Append(mb.Name);

            tbCurrLocation.Text = sb.ToString();

            //make sure content is visible
            ExpandContentPane();
        }

        void ExpandContentPane()
        {
            GridLength gl = columnContent.Width;

            if ((gl.IsAbsolute && gl.Value < 10.0) || columnContent.ActualWidth<10.0)
            {
                columnContent.Width = new GridLength(0.7,GridUnitType.Star);
                columnLeftPane.Width = new GridLength(0.3,GridUnitType.Star);
            }
        }

        void Navigated(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkContentElement)) return;

            try
            {
                FrameworkContentElement elem = (FrameworkContentElement)sender;
                MethodBase mb=null;
                int start=-1, end=Int32.MaxValue;

                if (elem.Tag == null) return;

                if (elem.Tag is MethodBase)
                {
                    //from method list
                    mb = (MethodBase)elem.Tag;
                }
                else if (elem.Tag is ClrStackFrameInfo)
                {
                    //from stack trace view
                    ClrStackFrameInfo frame = (ClrStackFrameInfo)elem.Tag;
                    mb = frame.Method;
                    start = frame.ILOffset;
                    end = frame.ILOffsetEnd;
                }

                if (mb == null) return;

                this.NavigateToMethod(mb, start, end);
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }

        public void NavigateToType(Type t)
        {
            if (t == null) return;

            ObservableCollection<MethodBase> methods = AssemblySource.LoadMethods(t);
            this.tlv = CilVisualization.VisualizeMethodList(methods, Navigated);
            cMethodsList.Child = this.tlv;

            string plaintext;
            UIElement elem = CilVisualization.VisualizeType(t, Navigated, out plaintext);
            gridContent.Children.Clear();
            gridContent.Children.Add(elem);
            tbMainContent.Text = plaintext;
            this.current_method = null;
            this.current_type = t;
            
            //display type location
            StringBuilder sb = new StringBuilder(1000);
            Assembly ass = t.Assembly;

            if (ass != null) sb.Append(ass.GetName().Name);
            else sb.Append("???");

            sb.Append(" / ");
            sb.Append(t.FullName);
            tbCurrLocation.Text = sb.ToString();

            //make sure content is visible
            ExpandContentPane();
        }

        public void NavigateToStackTrace(ClrThreadInfo th)
        {
            this.tlv = CilVisualization.VisualizeStackTrace(th, Navigated, null);
            cMethodsList.Child = this.tlv;
            gridContent.Children.Clear();
            tbMainContent.Text = String.Empty;
            tbCurrLocation.Text = "(Stack trace)";
        }

        public void Clear()
        {
            if (tlv != null) tlv.Clear();

            tbCurrLocation.Text = String.Empty;
            tbMainContent.Text = String.Empty;
            gridContent.Children.Clear();
            this.current_method = null;
        }

        public void SetLeftPaneWidth(int? w)
        {
            if (w == null) columnLeftPane.Width = new GridLength(1,GridUnitType.Star);
            else columnLeftPane.Width = new GridLength(w.Value);
        }

        public void SetContentWidth(int? w)
        {
            if (w == null) columnContent.Width = new GridLength(1,GridUnitType.Star);
            else columnContent.Width = new GridLength(w.Value);
        }

        public Type GetCurrentType()
        {
            return this.current_type;
        }

        public MethodBase GetCurrentMethod()
        {
            return this.current_method;
        }

        public string GetTextContent()
        {
            return this.tbMainContent.Text;
        }
    }
}
