/* CIL Tools 
 * Copyright (c) 2023, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Reflection;
using System.Collections.ObjectModel;
using CilTools.BytecodeAnalysis;
using CilTools.Runtime;
using CilView.Common;
using CilView.Core.DocumentModel;
using CilView.Visualization;

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
        Assembly current_assembly = null;
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

        void Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Uri == null) return;

            NavigationTarget target = CilVisualization.Server.ParseQueryString(e.Uri.Query);

            if (target == null) return;

            if (target.Kind == NavigationTargetKind.Instruction)
            {
                // Show instruction info dialog (don't actually navigate anywhere)
                e.Cancel = true;

                if (this.current_method == null) return;

                try
                {
                    CilInstruction targetInstruction = null;

                    foreach (CilInstruction instr in CilReader.GetInstructions(current_method))
                    {
                        if (instr.OrdinalNumber == target.InstructionNumber)
                        {
                            targetInstruction = instr;
                            break;
                        }
                    }

                    if (targetInstruction == null) return;

                    InstructionMenu.ShowInstructionDialog(targetInstruction);
                }
                catch (Exception ex)
                {
                    ErrorHandler.Current.Error(ex);
                }

                return;
            }

            MemberInfo targetMember = target.Member;

            if (targetMember == null) return;

            // Navigate to target member
            if (targetMember is MethodBase)
            {
                e.Cancel = true;
                this.NavigateToMethod((MethodBase)targetMember);
            }
            else if (targetMember is Type)
            {
                e.Cancel = true;
                this.NavigateToType((Type)targetMember, false);
            }
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
                else if (elem.Tag is CilInstruction)
                {
                    //from label hyperlink
                    CilInstruction instr = (CilInstruction)elem.Tag;
                    mb = instr.Method;
                    start = (int)instr.ByteOffset;
                    end = (int)(instr.ByteOffset + instr.TotalSize);
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
            CilBrowserPage page = new CilBrowserPage(mb, start, end, Navigating);
            page.Title = mb.Name;
            frameContent.Navigate(page);
            this.current_method = mb;
            this.text = page.ContentText;

            //make sure content is visible
            ExpandContentPane();
        }

        public ObservableCollection<MethodBase> NavigateToType(Type t, bool updateMethodList)
        {
            if (t == null) return new ObservableCollection<MethodBase>();

            string contenttext = String.Empty;

            try
            {
                CilBrowserPage page = new CilBrowserPage(t, Navigating);
                page.Title = "Type: " + t.Name;
                frameContent.Navigate(page);
                contenttext = page.ContentText;
            }
            catch (TypeLoadException ex) 
            {
                ErrorHandler.Current.Error(ex);
                frameContent.Navigate(String.Empty);
            }
            
            ObservableCollection<MethodBase> methods = AssemblySource.LoadMethods(t);

            if (updateMethodList)
            {
                //display method list in left pane
                this.tlv = CilVisualization.VisualizeMethodList(methods, Navigated);
                cMethodsList.Child = this.tlv;

                this.current_method = null;
                this.current_type = t;
            }

            this.text = contenttext;

            //make sure content is visible
            ExpandContentPane();

            return methods;
        }

        public void NavigateToAssembly(Assembly ass)
        {
            string contenttext = string.Empty;

            CilBrowserPage page = new CilBrowserPage(ass, Navigating);

            if (ass is IlasmAssembly)
            {
                IlasmAssembly ia = (IlasmAssembly)ass;
                page.Title = ia.Title;
            }
            else
            {
                page.Title = "Assembly: " + ass.GetName().Name;
            }
            
            frameContent.Navigate(page);
            contenttext = page.ContentText;
            
            this.current_method = null;
            this.current_type = null;
            this.current_assembly = ass;
            this.text = contenttext;

            //make sure content is visible
            ExpandContentPane();
        }

        public void NavigateToStackTrace(ClrThreadInfo th)
        {
            this.tlv = CilVisualization.VisualizeStackTrace(th, Navigated, null);
            cMethodsList.Child = this.tlv;
            
            frameContent.Navigate("(Stack trace)");
            this.text = String.Empty;
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

        public Assembly GetCurrentAssembly()
        {
            return this.current_assembly;
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

            if (m != null && m is MethodBase && this.tlv != null)
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
