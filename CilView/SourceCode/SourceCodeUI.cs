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
using CilTools.SourceCode;
using CilView.Common;
using CilView.Core.Syntax;
using CilView.UI.Dialogs;

namespace CilView.SourceCode
{
    static class SourceCodeUI
    {
        static HashSet<string> s_allowedRemoteServers = new HashSet<string>();

        static string GetHost(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                return uri.Host.ToLower();
            }
            catch (UriFormatException)
            {
                return string.Empty;
            }
        }

        static bool IsRemoteNavigationAllowed(string url)
        {
            string server = GetHost(url);

            if (string.IsNullOrEmpty(server)) return false;

            return s_allowedRemoteServers.Contains(server);
        }

        static void AllowRemoteNavigation(string url)
        {
            string server = GetHost(url);

            if (!string.IsNullOrEmpty(server)) s_allowedRemoteServers.Add(server);
        }

        static void ShowDecompiledSource(MethodBase method)
        {
            //stub implementation that only works for abstract methods
            IEnumerable<SourceToken> tokens = Decompiler.DecompileMethodSignature(".cs", method);
            DocumentViewWindow wnd = new DocumentViewWindow();
            wnd.Title = "Source code";
            wnd.Document = SourceVisualization.VisualizeTokens(tokens, string.Empty, "Source code from: Decompiler");
            wnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            wnd.ShowDialog();
        }

        static void ShowSourceWholeMethod(SourceDocument doc)
        {
            MethodSourceViewWindow wnd = new MethodSourceViewWindow();
            wnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            wnd.LoadDocument(doc);
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

                if (string.IsNullOrEmpty(doc.Text))
                {
                    //Local sources not available
                    string sourceLinkStr = doc.SourceLinkMap;

                    if (string.IsNullOrEmpty(sourceLinkStr))
                    {
                        MessageBox.Show("Failed to get source code: Source file " + doc.FilePath +
                            " is not found or empty", "Error");
                        return;
                    }

                    //Source Link
                    SourceLinkMap map = SourceLinkMap.Read(sourceLinkStr);
                    string serverPath = map.GetServerPath(doc.FilePath);

                    if (string.IsNullOrEmpty(serverPath))
                    {
                        MessageBox.Show("Failed to map file path " + doc.FilePath +
                            "into a source link server path", "Error");
                        return;
                    }

                    bool shouldNavigate = false;

                    if (IsRemoteNavigationAllowed(serverPath))
                    {
                        shouldNavigate = true;
                    }
                    else
                    {
                        string msg = string.Format(
                            "The source code is located on the remote server:\r\n{0}\r\n\r\n" +
                            "Would you like to allow navigation to files on this server?",
                            serverPath);

                        MessageBoxResult res = MessageBox.Show(msg, "Source Link information",
                            MessageBoxButton.YesNo);

                        if (res == MessageBoxResult.Yes)
                        {
                            shouldNavigate = true;
                            AllowRemoteNavigation(serverPath);
                        }
                    }

                    if (shouldNavigate)
                    {
                        Utils.ShellExecute(serverPath, null, "Failed to open source code URL");
                    }

                    return;
                }
                
                //Local sources
                if (wholeMethod)
                {
                    ShowSourceWholeMethod(doc);
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
