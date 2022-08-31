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
using CilTools.BytecodeAnalysis;
using CilTools.Runtime;
using CilView.Common;
using CilView.Core.Documents;

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
        string text = String.Empty;
        bool shouldClearHistory = false;
        int nav_counter = 0;

        public CilBrowser()
        {
            InitializeComponent();
        }

        void ExpandContentPane()
        {
            GridLength gl = columnContent.Width;

            if ((gl.IsAbsolute && gl.Value < 10.0) || columnContent.ActualWidth < 10.0)
            {
                columnContent.Width = new GridLength(0.7, GridUnitType.Star);
                columnLeftPane.Width = new GridLength(0.3, GridUnitType.Star);
            }
        }

        public void SetLeftPaneWidth(int? w)
        {
            if (w == null) columnLeftPane.Width = new GridLength(1, GridUnitType.Star);
            else columnLeftPane.Width = new GridLength(w.Value);
        }

        public void SetContentWidth(int? w)
        {
            if (w == null) columnContent.Width = new GridLength(1, GridUnitType.Star);
            else columnContent.Width = new GridLength(w.Value);
        }

        MethodBase ExtractMethod(FrameworkContentElement fce)
        {
            if (fce.Tag is MethodBase)
            {
                //from method list
                return (MethodBase)fce.Tag;
            }

            //from stack trace view
            MethodBase mb = null;
            ClrStackFrameInfo frame = fce.Tag as ClrStackFrameInfo;

            if (frame != null)
            {
                mb = frame.Method;
            }

            return mb;
        }

        void Navigated(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkContentElement)) return;

            try
            {
                FrameworkContentElement elem = (FrameworkContentElement)sender;
                MethodBase mb = null;
                int start = -1, end = Int32.MaxValue;

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

        public void NavigateToMethod(MethodBase mb)
        {
            this.NavigateToMethod(mb, -1, Int32.MaxValue);
        }

        public void NavigateToMethod(MethodBase mb, int start, int end)
        {
            CilBrowserPage page = new CilBrowserPage(mb, start, end, Navigated);
            page.Title = mb.Name;
            frameContent.Navigate(page);
            this.current_method = mb;
            this.text = page.ContentText;

            //make sure content is visible
            ExpandContentPane();
        }

        public ObservableCollection<MethodBase> NavigateToType(Type t)
        {
            if (t == null) return new ObservableCollection<MethodBase>();

            string contenttext = String.Empty;

            try
            {
                CilBrowserPage page = new CilBrowserPage(t, Navigated);
                page.Title = "Type: " + t.Name;
                frameContent.Navigate(page);
                contenttext = page.ContentText;
            }
            catch (TypeLoadException ex) 
            {
                ErrorHandler.Current.Error(ex);
                frameContent.Navigate(String.Empty);
            }

            //display method list in left pane
            ObservableCollection<MethodBase> methods = AssemblySource.LoadMethods(t);
            this.tlv = CilVisualization.VisualizeMethodList(methods, Navigated);
            cMethodsList.Child = this.tlv;
            
            this.current_method = null;
            this.current_type = t;
            this.text = contenttext;

            //make sure content is visible
            ExpandContentPane();

            return methods;
        }

        public void NavigateToStackTrace(ClrThreadInfo th)
        {
            this.tlv = CilVisualization.VisualizeStackTrace(th, Navigated, null);
            cMethodsList.Child = this.tlv;
            
            frameContent.Navigate("(Stack trace)");
            this.text = String.Empty;
        }

        public void NavigateToSourceDocument(DocumentAssembly content, string contentText, string filename)
        {
            CilBrowserPage page = new CilBrowserPage(content, contentText, filename);
            page.Title = filename;
            frameContent.Navigate(page);
            this.text = contentText;
            this.current_method = null;
            this.current_type = null;

            //make sure content is visible
            ExpandContentPane();
        }

        public void Clear()
        {
            if (tlv != null) tlv.Clear();

            this.shouldClearHistory = true;
            this.text = String.Empty;
            frameContent.Navigate(String.Empty);
            this.current_method = null;
            this.current_type = null;
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
            return this.text;
        }

        private void frameContent_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            //disable sound for current process to prevent Frame's navigation click sound
            Audio.SetMute(true);
            nav_counter++;
        }

        private async void frameContent_LoadCompleted(object sender, NavigationEventArgs e)
        {
            if (shouldClearHistory)
            {
                shouldClearHistory = false;

                while (true)
                {
                    if (frameContent.CanGoBack) frameContent.RemoveBackEntry();
                    else break;
                }
            }

            //update control properties for navigated page
            MemberInfo m = null;

            if (e.Content is CilBrowserPage)
            {
                CilBrowserPage page = (CilBrowserPage)e.Content;
                m = page.Member;
                this.text = page.ContentText;
            }
            else
            {
                this.text = String.Empty;
            }

            if (m != null && m is MethodBase)
            {
                MethodBase mb = null;
                mb = (MethodBase)m;
                this.current_method = mb;

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
                }//end for
            }
            else if (m != null && m is Type)
            {
                Type t = (Type)m;
                this.current_method = null;
                this.current_type = t;
            }
            else
            {
                this.current_method = null;
                this.current_type = null;
            }

            //enable sound back after delay
            await Task.Delay(250);
            nav_counter--;
            if(nav_counter<=0) Audio.SetMute(false);
        }
    }
}
