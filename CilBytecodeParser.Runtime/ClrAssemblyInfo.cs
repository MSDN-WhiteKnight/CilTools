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
    public class ClrAssemblyInfo:Assembly, ITokenResolver
    {
        static ClrAssemblyInfo unknown = new ClrAssemblyInfo(null);

        internal static ClrAssemblyInfo UnknownAssembly
        {
            get { return unknown; }
        }

        ClrModule module;
        AssemblyName asn;
        Dictionary<int, MemberInfo> table = new Dictionary<int, MemberInfo>();

        internal ClrAssemblyInfo(ClrModule m)
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

        internal void ClearTokenTable()
        {
            this.table.Clear();
        }

        internal void SetMemberByToken(int token, MemberInfo member)
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

        public IEnumerable<MemberInfo> EnumerateMembers()
        {
            foreach (MemberInfo member in table.Values)
            {
                yield return member;
            }
        }

        public IEnumerable<MethodBase> EnumerateMethods()
        {
            foreach (MemberInfo member in table.Values)
            {
                if (member is MethodBase) yield return (MethodBase)member;
            }
        }

        public override Type[] GetTypes()
        {
            List<Type> types = new List<Type>();

            foreach (MemberInfo member in table.Values)
            {
                if (member is Type) types.Add( (Type)member );
            }

            return types.ToArray();
        }

        public override Type GetType(string name)
        {
            return GetType(name, false, false);
        }

        public override Type GetType(string name, bool throwOnError)
        {
            return GetType(name, throwOnError, false);
        }

        public override Type GetType(string name, bool throwOnError, bool ignoreCase)
        {
            StringComparison comp;

            if (ignoreCase) comp = StringComparison.InvariantCultureIgnoreCase;
            else comp = StringComparison.InvariantCulture;

            foreach (MemberInfo member in table.Values)
            {
                if (member is Type)
                {
                    if (String.Equals(member.Name, name, comp)) return (Type)member;
                }
            }

            if (throwOnError) throw new TypeLoadException("Type " + name + " not found");
            else return null;
        }

        public override IEnumerable<Type> ExportedTypes
        {
            get
            {
                foreach (MemberInfo member in table.Values)
                {
                    if (member is ClrTypeInfo)
                    {
                        ClrTypeInfo t = (ClrTypeInfo)member;

                        if (t.InnerType.IsPublic) yield return t;
                    }
                }
            }
        }

        public override Type[] GetExportedTypes()
        {
            List<Type> ret = new List<Type>();

            foreach (Type t in ExportedTypes)
            {
                ret.Add(t);
            }

            return ret.ToArray();
        }

        public override bool IsDynamic
        {
            get
            {
                if (this.module == null) return false;
                else return this.module.IsDynamic;
            }
        }

        public override bool ReflectionOnly{ get { return true; } }

         
    }
}
