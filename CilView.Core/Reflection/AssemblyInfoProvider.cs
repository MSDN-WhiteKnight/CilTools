/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using CilTools.Reflection;
using CilView.Common;

namespace CilView.Core.Reflection
{
    public static class AssemblyInfoProvider
    {
        static void ArrayBytesToText(byte[] arr, TextWriter target)
        {
            if (arr == null) return;

            for (int i = 0; i < arr.Length; i++)
            {
                target.Write(arr[i].ToString("X").PadLeft(2, '0'));
                target.Write(' ');
            }

            target.Flush();
        }

        public static string GetReadableString(byte[] rawData)
        {
            StringBuilder sb = new StringBuilder(rawData.Length);

            for (int i = 0; i < rawData.Length; i++)
            {
                char c = (char)rawData[i];

                //include characters that can be displayed, but replace everything else with dots, 
                //like ildasm does for custom attributes data
                if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c) || c == ' ')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('.');
                }
            }

            return sb.ToString();
        }

        public static string GetAssemblyInfo(Assembly ass)
        {
            StringBuilder sb = new StringBuilder(1000);
            StringWriter wr = new StringWriter(sb);

            // Assembly name
            AssemblyName an = ass.GetName();
            wr.WriteLine("Name: " + an.Name);

            if (an.Version != null)
            {
                wr.WriteLine("Version: " + an.Version.ToString());
            }

            if (!string.IsNullOrEmpty(an.CultureName))
            {
                wr.WriteLine("Culture: " + an.CultureName);
            }

            byte[] token = an.GetPublicKeyToken();

            if (token != null)
            {
                wr.Write("Public key token: ");
                ArrayBytesToText(token, wr);
                wr.WriteLine();
            }

            wr.WriteLine("Location: " + ass.Location);

            // File info
            FileInfo fi = null;

            try
            {
                if (!string.IsNullOrEmpty(ass.Location)) fi = new FileInfo(ass.Location);
            }
            catch (Exception ex)
            {
                wr.WriteLine("Failed to get file info");
                wr.WriteLine(ex.ToString());
            }

            if (fi != null)
            {
                wr.WriteLine("File size: " + Math.Round(fi.Length / 1024.0f, 2).ToString() + " KB");
                wr.WriteLine("Created: " + fi.CreationTime.ToString());
                wr.WriteLine("Modified: " + fi.LastWriteTime.ToString());
            }

            wr.WriteLine();

            // PE info
            string peinfo = ReflectionInfoProperties.GetProperty(ass, ReflectionInfoProperties.InfoText) as string;

            if (peinfo != null)
            {
                wr.WriteLine(peinfo);
            }

            // Assembly custom attributes
            try
            {
                object[] attrs = ass.GetCustomAttributes(false);

                if (attrs == null) attrs = new object[0];

                if (attrs.Length > 0)
                {
                    wr.WriteLine("    Assembly custom attributes");
                    wr.WriteLine();
                }

                for (int i = 0; i < attrs.Length; i++)
                {
                    if (!(attrs[i] is ICustomAttribute)) continue;

                    ICustomAttribute ica = (ICustomAttribute)attrs[i];
                    string name = ica.Constructor.DeclaringType.Name;
                    byte[] rawData = ica.Data;
                    string textData = GetReadableString(rawData);

                    wr.WriteLine(name);
                    ArrayBytesToText(rawData, wr);
                    wr.WriteLine();
                    wr.WriteLine("(" + textData + ")");
                    wr.WriteLine();
                }
            }
            catch (Exception ex)
            {
                wr.WriteLine("Failed to get assembly custom attributes");
                wr.WriteLine(ex.ToString());
            }

            wr.Flush();
            return sb.ToString();
        }
    }
}
