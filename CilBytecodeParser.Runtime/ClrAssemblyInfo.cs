/* CilBytecodeParser library 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilBytecodeParser;
using CilBytecodeParser.Reflection;
using Microsoft.Diagnostics.Runtime;

namespace CilBytecodeParser.Runtime
{
    class ClrAssemblyInfo:Assembly, ITokenResolver
    {
        ClrModule module;
        AssemblyName asn;
        Dictionary<int, MemberInfo> table = new Dictionary<int, MemberInfo>();

        public ClrAssemblyInfo(ClrModule m)
        {
            this.module = m;
            AssemblyName n = new AssemblyName();

            if (m != null)
            {

                n.Name = Path.GetFileNameWithoutExtension(this.module.AssemblyName);

                if (this.module.IsFile) n.CodeBase = this.module.FileName;
                else n.CodeBase = "";
            }
            else
            {
                n.Name = "???";
                n.CodeBase = "";
            }

            this.asn = n;
        }

        public ClrModule InnerModule
        {
            get { return this.module; }
        }

        public override string FullName
        {
            get
            {
                return this.asn.FullName;
            }
        }

        public override AssemblyName GetName()
        {
            return this.asn;
        }

        public override string Location
        {
            get
            {
                return this.asn.CodeBase;
            }
        }

        public override string CodeBase
        {
            get
            {
                return this.asn.CodeBase;
            }
        }

        public void Clear()
        {
            this.table.Clear();
        }

        public void SetValue(int token, MemberInfo member)
        {
            table[token] = member;
        }

        public Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (table.ContainsKey(metadataToken)) return table[metadataToken] as Type;
            else return null;
        }

        public Type ResolveType(int metadataToken)
        {
            return this.ResolveType(metadataToken, null, null);
        }

        public MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (table.ContainsKey(metadataToken)) return table[metadataToken] as MethodBase;
            else return null;
        }

        public MethodBase ResolveMethod(int metadataToken)
        {
            return this.ResolveMethod(metadataToken, null, null);
        }

        public FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (table.ContainsKey(metadataToken)) return table[metadataToken] as FieldInfo;
            else return null;
        }

        public FieldInfo ResolveField(int metadataToken)
        {
            return this.ResolveField(metadataToken, null, null);
        }

        public MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (table.ContainsKey(metadataToken)) return table[metadataToken];
            else return null;
        }

        public MemberInfo ResolveMember(int metadataToken)
        {
            return this.ResolveMember(metadataToken, null, null);
        }

        public byte[] ResolveSignature(int metadataToken)
        {
            throw new NotImplementedException();
        }

        public string ResolveString(int metadataToken)
        {
            throw new NotImplementedException();
        }
    }
}
