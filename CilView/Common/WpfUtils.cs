/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace CilView.Common
{
    public static class WpfUtils
    {
        public static void DoWpfEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                 new DispatcherOperationCallback((f) =>
                 {
                     ((DispatcherFrame)f).Continue = false; return null;
                 }), frame);
            Dispatcher.PushFrame(frame);
        }
        
        /// <summary>
        /// Opens the specified file or URL in the default associated application. 
        /// Does not throw exceptions on failure.
        /// </summary>
        /// <param name="filepath">File or URL to open</param>
        public static void ShellExecute(string filepath, Window wnd, string errmsg)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = filepath;
                psi.UseShellExecute = true;
                Process pr = Process.Start(psi);
                if (pr != null) pr.Dispose();
            }
            catch (Exception ex)
            {
                ErrorHandler.Current.Error(ex, "WpfUtils.ShellExecute", silent: true);

                MessageBox.Show(wnd,
                    errmsg + Environment.NewLine + Environment.NewLine + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
