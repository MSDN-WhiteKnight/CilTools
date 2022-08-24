/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using CilTools.Reflection;

namespace CilView.Core.Reflection
{
    public static class AssemblyInfoProvider
    {
        static void ArrayBytesToText(byte[] arr, TextWriter target)
        {
            if (arr == null) return;

            for (int i = 0; i < arr.Length; i++)
            {
                target.Write(arr[i].ToString("X"));
                target.Write(' ');
            }

            target.Flush();
        }

        public static string GetAssemblyInfo(Assembly ass)
        {
            StringBuilder sb = new StringBuilder(1000);
            StringWriter wr = new StringWriter(sb);
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
            wr.WriteLine();

            string peinfo = ReflectionInfoProperties.GetProperty(ass, ReflectionInfoProperties.InfoText) as string;

            if (peinfo != null)
            {
                wr.WriteLine(peinfo);
            }

            wr.Flush();
            return sb.ToString();
        }
    }
}
