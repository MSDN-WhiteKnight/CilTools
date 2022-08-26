/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CilTools.Reflection;

namespace CilView.Common
{
    public static class Utils
    {
        public static bool StringEquals(string left, string right)
        {
            return String.Equals(left, right, StringComparison.InvariantCulture);
        }

        public static bool StringEqualsIgnoreCase(string left, string right)
        {
            return String.Equals(left, right, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool PathEquals(string left, string right)
        {
            if (left == null)
            {
                if (right == null) return true;
                else return false;
            }

            if (right == null) return false;

            if (left.EndsWith("\\") || left.EndsWith("/"))
            {
                left = left.Substring(0, left.Length - 1);
            }

            if (right.EndsWith("\\") || right.EndsWith("/"))
            {
                right = right.Substring(0, right.Length - 1);
            }

            left = left.ToLower();
            right = right.ToLower();
            return StringEquals(left,right);
        }

        public static int Search<T>(T[] array, Func<T, string, bool> func, string text, int start_index)
        {
            if (start_index < 0) start_index = 0;
            if (start_index >= array.Length) return -1;

            for (int i = start_index; i < array.Length; i++)
            {
                if (func(array[i], text))
                {
                    return i; //found item
                }
            }

            return -1; //not found
        }
        
        public static async Task RunInBackground(Action action)
        {
            Exception err = null;

            await Task.Run(() => {
                try { action(); }
                catch (Exception ex)
                {
                    err = ex;
                }
            });

            if (err != null) throw err;
        }

        public static string GetReadableString(string x)
        {
            StringBuilder sb = new StringBuilder(x.Length);

            for(int i = 0; i < x.Length; i++)
            {
                char c = x[i];

                if (Char.IsLetterOrDigit(c) || Char.IsPunctuation(c) || 
                    Char.IsWhiteSpace(c) || c=='=')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(' ');
                }
            }

            return sb.ToString();
        }

        public static MemberInfo[] GetAllMembers(Type t)
        {
            return t.GetMembers(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|
                BindingFlags.Instance);
        }

        public static bool IsMethodAndNotConstructor(MemberInfo m)
        {
            MethodBase mb = m as MethodBase;
            if (mb == null) return false;

            return mb.MemberType == MemberTypes.Method && !StringEquals(mb.Name,".ctor") 
                && !StringEquals(mb.Name, ".cctor");
        }

        public static bool IsConstructor(MemberInfo m)
        {
            MethodBase mb = m as MethodBase;
            if (mb == null) return false;

            return StringEquals(mb.Name, ".ctor")|| StringEquals(mb.Name, ".cctor");
        }

        public static bool IsPropertyMethod(MethodBase m)
        {
            return m.IsSpecialName && (m.Name.StartsWith("get_") || m.Name.StartsWith("set_"));
        }

        public static bool IsAbstractInterfaceMethod(MethodBase m)
        {
            try
            {
                if (m.IsStatic) return false;
                if(!m.IsAbstract) return false;

                Type t = m.DeclaringType;

                if (t == null) return false;

                return t.IsInterface;
            }
            catch (NotImplementedException) { return false; }
            catch (NotSupportedException) { return false; }
        }

        public static int GetMethodBodySize(MethodBase mb)
        {
            if (mb.IsAbstract) return -1;
            if (!(mb is ICustomMethod)) return -1;

            try
            {
                ICustomMethod cm = (ICustomMethod)mb;
                byte[] body = cm.GetBytecode();
                if (body.Length > 0) return body.Length;
                else return -1;
            }
            catch (Exception)
            {
                return -1;
            }
        }
        
        public static bool TypeEquals(Type left, Type right)
        {
            if (left == null)
            {
                if (right == null) return true;
                else return false;
            }

            if (right == null) return false;

            return StringEquals(left.FullName, right.FullName);
        }
    }
}
