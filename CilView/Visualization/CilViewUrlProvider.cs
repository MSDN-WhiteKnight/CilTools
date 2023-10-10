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
        static MemberInfo ResolveMember(MemberInfo member)
        {
            // Find defininition for generic types/methods
            if (member is Type)
            {
                Type t = (Type)member;

                if (t.IsGenericType && !t.IsGenericTypeDefinition)
                {
                    Type tDefinition = t.GetGenericTypeDefinition();

                    if (tDefinition != null) member = tDefinition;
                }
            }
            else if (member is MethodInfo && member is ICustomMethod)
            {
                MethodInfo m = (MethodInfo)member;
                ICustomMethod cm = (ICustomMethod)member;

                if (m.IsGenericMethod && !m.IsGenericMethodDefinition)
                {
                    MethodBase mDefinition = cm.GetDefinition();

                    if (mDefinition != null) member = mDefinition;
                }
            }

            // Find target member for references
            MemberInfo target = ReflectionProperties.Get(member, ReflectionProperties.ReferenceTarget) as MemberInfo;

            if (target != null) return target;
            else return member;
        }

        public override string GetMemberUrl(MemberInfo member)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                if (member is MethodBase)
                {
                    MethodBase mb = (MethodBase)member;
                    MethodBase mTarget = ResolveMember(mb) as MethodBase;

                    if (mTarget == null) return string.Empty;
                    if (mTarget.DeclaringType == null) return string.Empty;

                    string name = Utils.GetAssemblySimpleName(mTarget.DeclaringType.Assembly);
                    sb.Append(ServerBase.DefaultUrlHost + ServerBase.DefaultUrlPrefix + "render.html?");

                    if (!string.IsNullOrEmpty(name)) sb.Append("assembly=" + WebUtility.UrlEncode(name) + "&");

                    sb.Append("token=" + mTarget.MetadataToken.ToString("X", CultureInfo.InvariantCulture));
                    return sb.ToString();
                }
                else if (member is Type)
                {
                    Type t = (Type)member;
                    Type tTarget = ResolveMember(t) as Type;

                    if (tTarget == null) return string.Empty;

                    string name = Utils.GetAssemblySimpleName(tTarget.Assembly);
                    sb.Append(ServerBase.DefaultUrlHost + ServerBase.DefaultUrlPrefix + "render.html?");

                    if (!string.IsNullOrEmpty(name)) sb.Append("assembly=" + WebUtility.UrlEncode(name) + "&");

                    sb.Append("token=" + tTarget.MetadataToken.ToString("X", CultureInfo.InvariantCulture));
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
