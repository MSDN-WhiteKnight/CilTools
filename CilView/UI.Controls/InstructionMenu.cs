/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CilTools.BytecodeAnalysis;
using CilTools.Syntax;
using CilView.Symbols;
using CilView.UI.Dialogs;

namespace CilView.UI.Controls
{
    /// <summary>
    /// Represents context menu displayed when right-clicking instruction opcode in CilBrowser's 
    /// formatted view
    /// </summary>
    static class InstructionMenu
    {
        static ContextMenu s_instructionMenu;
        static FrameworkContentElement s_instructionMenuTarget;

        /// <summary>
        /// Gets the single instance of instruction context menu
        /// </summary>
        public static ContextMenu GetInstructionMenu()
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
            menu.Items.Add(new Separator());

            mi = new MenuItem();
            mi.Header = "Copy";
            mi.InputGestureText = "Ctrl+C";
            mi.Click += Mi_Copy_Click;
            menu.Items.Add(mi);
            mi = new MenuItem();
            mi.Header = "Select all";
            mi.InputGestureText = "Ctrl+A";
            mi.Click += Mi_SelectAll_Click;
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
                    string methodstr = string.Empty;

                    try { methodstr = PdbUtils.GetMethodSigString(srcinfo.Method); }
                    catch (Exception ex)
                    {
                        //don't error out if we can't build good signature string
                        ErrorHandler.Current.Error(ex, "", silent: true);
                        methodstr = CilVisualization.MethodToString(srcinfo.Method);
                    }

                    //build display string
                    StringBuilder sb = new StringBuilder(src.Length * 2);
                    sb.AppendFormat("({0}, ", srcinfo.SourceFile);
                    sb.AppendFormat("lines {0}-{1})", srcinfo.LineStart, srcinfo.LineEnd);
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine(methodstr);
                    sb.AppendLine(PdbUtils.Deindent(src));
                    sb.AppendLine();

                    //show source code
                    TextViewWindow wnd = new TextViewWindow();
                    wnd.Title = "Source code";
                    wnd.Text = sb.ToString();
                    wnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    wnd.TextFontFamily = new FontFamily("Courier New");
                    wnd.TextFontSize = 14.0;
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
            if (syntax == null) return;
            if (syntax.Instruction == null) return;

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

        static FlowDocumentScrollViewer GetViewer()
        {
            if (s_instructionMenu == null) return null;

            FlowDocumentScrollViewer el = s_instructionMenu.PlacementTarget as FlowDocumentScrollViewer;
            return el;
        }

        private static void Mi_Copy_Click(object sender, RoutedEventArgs e)
        {
            FlowDocumentScrollViewer el = GetViewer();
            if (el == null) return;

            TextSelection sel = el.Selection;
            if (sel == null) return;

            string text = sel.Text;
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                //copy current selection to clipboard
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }

        private static void Mi_SelectAll_Click(object sender, RoutedEventArgs e)
        {
            FlowDocumentScrollViewer el = GetViewer();
            if (el == null) return;

            TextSelection sel = el.Selection;
            if (sel == null) return;

            try
            {
                //select all content of the current document
                sel.Select(el.Document.ContentStart, el.Document.ContentEnd);
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex);
            }
        }

        internal static void R_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            //saves the content element that user right-clicked on, so we can figure out later
            //which instruction this context menu points to
            s_instructionMenuTarget = sender as FrameworkContentElement;
        }
    }
}
