/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CilView.UI.Dialogs;

namespace CilView
{
    public class ErrorHandler
    {
        public const string UserFilesCatalog = "CilTools_UserFiles";

        protected string _logfile;

        protected static ErrorHandler _current = new ErrorHandler();

        public static ErrorHandler Current
        {
            get { return _current; }
        }

        public ErrorHandler(string lf)
        {
            this._logfile = lf;
        }

        public ErrorHandler()
        {
            try
            {
                string path = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                        UserFilesCatalog);
                this._logfile = System.IO.Path.Combine(path, "error.log");
                Directory.CreateDirectory(path);
            }
            catch (Exception){}
        }

        public void Error(Exception ex)
        {
            Error(ex, null, false);
        }

        public void Error(Exception ex, string action = null)
        {
            Error(ex, action, false);
        }

        public void Error(Exception ex, string action = null, bool silent = false)
        {
            if (action == null) action = "";
            if (ex == null) ex = new ApplicationException("Unknown error");

            try
            {
                /*Запись ошибки в файл отчета*/
                StreamWriter wr = new StreamWriter(_logfile, true);
                using (wr)
                {

                    wr.WriteLine("*** " + DateTime.Now.ToString() + " ***");
                    if (action.Length > 0)
                    {
                        wr.WriteLine("on action *" + action + "*");
                    }
                    wr.WriteLine(ex.ToString());
                    wr.WriteLine("Module: " + ex.Source);
                    wr.WriteLine("Method: " + ex.TargetSite);

                    if (ex.HelpLink != null)
                    {
                        wr.WriteLine("Help file: " + ex.HelpLink);
                    }

                    if (ex.Data != null && ex.Data.Count > 0)
                    {
                        wr.WriteLine(" --- Additional data ---");
                        foreach (object val in ex.Data.Keys)
                        {
                            wr.WriteLine(val + ": " + ex.Data[val]);
                        }
                        wr.WriteLine("------------------------");
                    }

                    wr.WriteLine("***********************************");
                    wr.WriteLine();
                }
            }
            catch (Exception) { ; }

            if (!silent)
            {
                System.Media.SystemSounds.Exclamation.Play();//воспроизвести звук ошибки

                /*Вывод окна с сообщением об ошибке*/
                wndError w = new wndError(ex, action);
                w.ShowDialog();
            }
        }
    }
}
