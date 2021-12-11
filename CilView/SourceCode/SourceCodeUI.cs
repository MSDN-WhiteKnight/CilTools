/* CIL Tools 
 * Copyright (c) 2021,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using CilView.Common;
using CilView.UI.Dialogs;

namespace CilView.SourceCode
{
    static class SourceCodeUI
    {
        static void ShowDecompiledSource(MethodBase method)
        {
            SourceInfo srcinfo;
            srcinfo = Decompiler.GetSourceFromDecompiler(method);

            //build display string
            string src = srcinfo.SourceCode;
            StringBuilder sb = new StringBuilder(src.Length * 2);            
            sb.AppendLine(src);

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
                SourceInfo srcinfo;

                if (method.IsAbstract)
                {
                    //abstract method has no sequence points in PDB, just use decompiler
                    ShowDecompiledSource(method);
                    return;
                }
                
                //from PDB
                if (wholeMethod)
                {
                    srcinfo = PdbUtils.GetSourceFromPdb(method,
                        0, uint.MaxValue, SymbolsQueryType.RangeExact);
                }
                else
                {
                    srcinfo = PdbUtils.GetSourceFromPdb(method,
                        byteOffset, byteOffset, SymbolsQueryType.SequencePoint);
                }

                string src = srcinfo.SourceCode;

                if (string.IsNullOrWhiteSpace(src))
                {
                    string str = "Failed to get source code";

                    if (srcinfo.Error != SourceInfoError.Success)
                    {
                        str += ": "+srcinfo.Error.ToString();
                    }

                    MessageBox.Show(str, "Error");
                    return;
                }

                if (wholeMethod)
                {
                    string methodstr = string.Empty;
                    string ext = Path.GetExtension(srcinfo.SourceFile);

                    try
                    {
                        if (!Utils.IsConstructor(srcinfo.Method) && !Utils.IsPropertyMethod(srcinfo.Method))
                        {
                            methodstr = Decompiler.DecompileMethodSignature(ext, srcinfo.Method);
                        }
                    }
                    catch (Exception ex)
                    {
                        //don't error out if we can't build good signature string
                        ErrorHandler.Current.Error(ex, "PdbUtils.GetMethodSigString", silent: true);
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

                    if (Decompiler.IsCppExtension(ext))
                    {
                        //C++ PDB sequence points don't include the trailing brace for some reason
                        sb.AppendLine("}");
                    }

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
            }//end try
        }
    }
}
