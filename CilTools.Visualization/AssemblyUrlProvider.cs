/* CIL Tools 
 * Copyright (c) 2023,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace CilTools.Visualization
{
    class AssemblyUrlProvider : UrlProviderBase
    {
        Assembly scope;
        
        public AssemblyUrlProvider(Assembly ass)
        {
            this.scope = ass;
        }

        public override string GetMemberUrl(MemberInfo member, int level)
        {
            if (member is MethodBase)
            {
                MethodBase mb = (MethodBase)member;

                if (mb.DeclaringType == null) return string.Empty;

                if (mb.DeclaringType.Assembly != this.scope) return string.Empty;
                else return "?token=" + mb.MetadataToken.ToString("X", CultureInfo.InvariantCulture);
            }
            else if (member is Type)
            {
                Type t = (Type)member;

                if (t.Assembly != this.scope) return string.Empty;
                else return "?token=" + t.MetadataToken.ToString("X", CultureInfo.InvariantCulture);
            }
            else return string.Empty;
        }
    }
}
