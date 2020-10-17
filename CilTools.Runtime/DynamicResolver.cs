/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CilTools.Reflection;

namespace CilTools.Runtime
{
    internal class DynamicResolver:ITokenResolver
    {
        Dictionary<int, MemberInfo> _table;

        public DynamicResolver(Dictionary<int, MemberInfo> table)
        {
            this._table = table;
        }

        public FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return null;
        }

        public FieldInfo ResolveField(int metadataToken)
        {
            return null;
        }

        public MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (this._table.ContainsKey(metadataToken)) return this._table[metadataToken];
            else return null;
        }

        public MemberInfo ResolveMember(int metadataToken)
        {
            return this.ResolveMember(metadataToken, null, null);
        }

        public MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            MethodBase ret; 

            if (this._table.ContainsKey(metadataToken)) ret = this._table[metadataToken] as MethodBase;
            else ret = null;

            return ret;
        }

        public MethodBase ResolveMethod(int metadataToken)
        {
            return this.ResolveMethod(metadataToken, null, null);
        }

        public byte[] ResolveSignature(int metadataToken)
        {
            return null;
        }

        public string ResolveString(int metadataToken)
        {
            return null;
        }

        public Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return null;
        }

        public Type ResolveType(int metadataToken)
        {
            return null;
        }
    }
}
