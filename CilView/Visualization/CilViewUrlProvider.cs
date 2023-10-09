/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Net;
using System.Text;
using CilTools.Visualization;
using CilView.Common;

namespace CilView.Visualization
{
    class CilViewUrlProvider : UrlProviderBase
    {
        public override string GetMemberUrl(MemberInfo member)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                if (member is MethodBase)
                {
                    MethodBase mb = (MethodBase)member;

                    if (mb.DeclaringType == null) return string.Empty;

                    string name = Utils.GetAssemblySimpleName(mb.DeclaringType.Assembly);
                    sb.Append(ServerBase.DefaultUrlHost + ServerBase.DefaultUrlPrefix + "render.html?");

                    if (!string.IsNullOrEmpty(name)) sb.Append("assembly=" + WebUtility.UrlEncode(name) + "&");

                    sb.Append("token=" + mb.MetadataToken.ToString("X", CultureInfo.InvariantCulture));
                    return sb.ToString();
                }
                else if (member is Type)
                {
                    Type t = (Type)member;

                    string name = Utils.GetAssemblySimpleName(t.Assembly);
                    sb.Append(ServerBase.DefaultUrlHost + ServerBase.DefaultUrlPrefix + "render.html?");

                    if (!string.IsNullOrEmpty(name)) sb.Append("assembly=" + WebUtility.UrlEncode(name) + "&");

                    sb.Append("token=" + t.MetadataToken.ToString("X", CultureInfo.InvariantCulture));
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
