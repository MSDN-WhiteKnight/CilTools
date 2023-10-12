/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Net;
using System.Text;
using CilTools.Reflection;
using CilTools.Visualization;
using CilView.Common;

namespace CilView.Visualization
{
    class CilViewUrlProvider : UrlProviderBase
    {
        static Assembly GetContainingAssembly(MemberInfo member)
        {
            Assembly ca = ReflectionProperties.Get(member, ReflectionProperties.ContainingAssembly) as Assembly;

            if (ca != null) return ca;

            if (member is Type)
            {
                return ((Type)member).Assembly;
            }
            else
            {
                if (member.DeclaringType == null) return null;
                else return member.DeclaringType.Assembly;
            }
        }

        public override string GetMemberUrl(MemberInfo member)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                if (member is MethodBase || member is Type)
                {
                    Assembly containingAssembly = GetContainingAssembly(member);
                    string name = Utils.GetAssemblySimpleName(containingAssembly);
                    sb.Append(ServerBase.DefaultUrlHost + ServerBase.DefaultUrlPrefix + "render.html?");

                    if (!string.IsNullOrEmpty(name)) sb.Append("assembly=" + WebUtility.UrlEncode(name) + "&");

                    sb.Append("token=" + member.MetadataToken.ToString("X", CultureInfo.InvariantCulture));
                    return sb.ToString();
                }
                else return string.Empty;
            }
            catch (InvalidOperationException)
            {
                return string.Empty; //dynamic methods throw when trying to get token
            }
        }
    }
}
