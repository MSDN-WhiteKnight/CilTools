/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CilView.UI.Dialogs
{
    public partial class wndError : Window
    {
        public wndError(Exception ex, string action="")
        {
            InitializeComponent();
            
            if (action == null) action = "";
            if (ex == null) ex = new ApplicationException("Unknown error");

            try
            {
                //заполнение краткого сообщения об ошибке
                txtErrorMessage.Text = "";
                if (action.Length > 0)
                {
                    txtErrorMessage.Text += "Error occured during *" + action + "*";
                    txtErrorMessage.Text += Environment.NewLine;
                }
                txtErrorMessage.Text += ex.GetType() + ": " + ex.Message;
                                
                //заполнение подробной информации
                StringBuilder b = new StringBuilder(300);
                b.AppendLine(ex.ToString());
                b.AppendLine("Module: " + ex.Source);
                b.AppendLine("Method: " + ex.TargetSite);

                if (ex.HelpLink != null)
                {
                    b.AppendLine("Help file: " + ex.HelpLink);
                }

                if (ex.Data != null && ex.Data.Count > 0)
                {
                    b.AppendLine(" --- Additional data ---");

                    foreach (object val in ex.Data.Keys)
                    {
                        b.AppendLine(val + ": " + ex.Data[val]);
                    }

                    b.AppendLine("------------------------");
                }
                txtErrorDetails.Text = b.ToString();
                
            }
            catch (Exception) {}
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
