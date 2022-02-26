/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using CilTools.SourceCode;
using CilView.Common;
using CilView.UI.Dialogs;

namespace CilView.SourceCode
{
    static class SourceCodeUI
    {
        static void ShowDecompiledSource(MethodBase method)
        {
            //stub implementation that only works for abstract methods
            string src = Decompiler.DecompileMethodSignature(".cs", method);

            //build display string
            StringBuilder sb = new StringBuilder(src.Length * 2);            
            sb.AppendLine(src);
            sb.AppendLine();
            sb.AppendLine("Source code from: Decompiler");

            //show source code
            TextViewWindow wnd = new TextViewWindow();
            wnd.Title = "Source code";
            wnd.Text = sb.ToString();
            wnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            wnd.TextFontFamily = new FontFamily("Courier New");
            wnd.TextFontSize = 14.0;
            wnd.ShowDialog();
        }

        /// <summary>
        /// Creates and shows View source window to display the source code of the specified method
        /// </summary>
        /// <param name="method">Method to show source code</param>
        /// <param name="byteOffset">Byte offset of the sequence point</param>
        /// <param name="wholeMethod">
        /// <c>true</c> to show source code of the whole method, <c>false</c> to show source code for 
        /// a single sequence point
        /// </param>
        public static void ShowSource(MethodBase method, uint byteOffset, bool wholeMethod)
        {
            try
            {
                if (method.IsAbstract)
                {
                    //abstract method has no sequence points in PDB, just use decompiler
                    ShowDecompiledSource(method);
                    return;
                }

                //from PDB
                PdbCodeProvider provider = PdbCodeProvider.Instance;
                SourceDocument doc = provider.GetSourceCodeDocuments(method).FirstOrDefault();
                
                if (doc == null)
                {
                    MessageBox.Show("Failed to get source code: line info not found for this method", "Error");
                    return;
                }
                
                if (wholeMethod)
                {
                    string src = doc.Text;
                    string methodstr = string.Empty;
                    string ext = Path.GetExtension(doc.FilePath);

                    try
                    {
                        if (!Utils.IsConstructor(doc.Method) && !Utils.IsPropertyMethod(doc.Method))
                        {
                            methodstr = Decompiler.DecompileMethodSignature(ext, doc.Method);
                        }
                    }
                    catch (Exception ex)
                    {
                        //don't error out if we can't build good signature string
                        ErrorHandler.Current.Error(ex, "PdbUtils.GetMethodSigString", silent: true);
                        methodstr = CilVisualization.MethodToString(doc.Method);
                    }

                    //build display string
                    StringBuilder sb = new StringBuilder(src.Length * 2);
                    sb.AppendFormat("({0}, ", doc.FilePath);
                    sb.AppendFormat("lines {0}-{1})", doc.LineStart, doc.LineEnd);
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine(methodstr);
                    sb.AppendLine(PdbUtils.Deindent(src));

                    if (Decompiler.IsCppExtension(ext))
                    {
                        //C++ PDB sequence points don't include the trailing brace for some reason
                        sb.AppendLine("}");
                    }

                    sb.AppendLine();
                    sb.Append("Symbols file: ");
                    sb.Append(doc.SymbolsFile);
                    sb.Append(" (");
                    sb.Append(doc.SymbolsFileFormat);
                    sb.Append(')');

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
                    SourceFragment f = PdbUtils.FindFragment(doc.Fragments, byteOffset);

                    if (f == null)
                    {
                        MessageBox.Show("Failed to get source code: sequence point not found", "Error");
                        return;
                    }

                    SourceViewWindow srcwnd = new SourceViewWindow(f);
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
            }//end try
        }
    }
}
