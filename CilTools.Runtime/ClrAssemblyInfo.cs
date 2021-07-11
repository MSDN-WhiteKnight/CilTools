/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;
using Microsoft.Diagnostics.Runtime;

namespace CilTools.Runtime
{
    /// <summary>
    /// Represents an assembly in the external CLR instance
    /// </summary>
    public class ClrAssemblyInfo:Assembly, ITokenResolver
    {
        static ClrAssemblyInfo unknown = new ClrAssemblyInfo(null,null);

        internal static ClrAssemblyInfo UnknownAssembly
        {
            get { return unknown; }
        }

        ClrModule module;
        AssemblyName asn;
        Dictionary<int, MemberInfo> table = new Dictionary<int, MemberInfo>();
        ClrAssemblyReader reader;

        internal ClrAssemblyInfo(ClrModule m, ClrAssemblyReader r)
        {
            this.module = m;
            AssemblyName n = new AssemblyName();

            if (m != null)
            {
                n.Name = Path.GetFileNameWithoutExtension(this.module.AssemblyName);

                if (String.IsNullOrEmpty(m.Name))
                {
                    if (m.IsDynamic)
                    {
                        //try to get dynamic module name from heap scan data
                        string dname=r.GetDynamicModuleName(m.Address);

                        if (!String.IsNullOrEmpty(dname)) n.Name = dname+" (dynamic)";
                        else n.Name = "<DynamicAssembly>";
                    }
                    else n.Name = "???";
                }

                if (this.module.IsFile) n.CodeBase = this.module.FileName;
                else n.CodeBase = "";
            }
            else
            {
                n.Name = "???";
                n.CodeBase = "";
            }

            this.asn = n;
            this.reader = r;
        }

        /// <summary>
        /// Gets the underlying ClrMD module object
        /// </summary>
        public ClrModule InnerModule
        {
            get { return this.module; }
        }

        /// <summary>
        /// Gets the assembly reader that was used to read this instance
        /// </summary>
        public ClrAssemblyReader AssemblyReader { get { return this.reader; } }

        /// <summary>
        /// Gets the display name of the assembly
        /// </summary>
        public override string FullName
        {
            get
            {
                return this.asn.FullName;
            }
        }

        /// <summary>
        /// Gets an AssemblyName for this assembly
        /// </summary>
        /// <returns>An object that contains the fully parsed display name for this assembly</returns>
        public override AssemblyName GetName()
        {
            return this.asn;
        }

        /// <summary>
        /// Gets the full path to the PE file containing this assembly, or an empty string if the assembly wasn't loaded from file.
        /// </summary>
        public override string Location
        {
            get
            {
                return this.asn.CodeBase;
            }
        }

        /// <summary>
        /// Gets the full path to the PE file containing this assembly, or an empty string if the assembly wasn't loaded from file.
        /// </summary>
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

        /// <summary>
        /// Returns the type identified by the specified metadata token, in the context defined by the specified generic parameters.
        /// </summary>
        /// <remarks>Generic parameters are ignored in this implementation.</remarks>
        public Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (table.ContainsKey(metadataToken)) return table[metadataToken] as Type;
            else return null;
        }

        /// <summary>
        /// Returns the type identified by the specified metadata token.
        /// </summary>
        public Type ResolveType(int metadataToken)
        {
            return this.ResolveType(metadataToken, null, null);
        }

        /// <summary>
        /// Returns the method or constructor identified by the specified metadata token, in the context defined by the 
        /// specified generic parameters.
        /// </summary>
        /// <remarks>Generic parameters are ignored in this implementation.</remarks>
        public MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (table.ContainsKey(metadataToken)) return table[metadataToken] as MethodBase;
            else return null;
        }

        /// <summary>
        /// Returns the method or constructor identified by the specified metadata token.
        /// </summary>
        public MethodBase ResolveMethod(int metadataToken)
        {
            return this.ResolveMethod(metadataToken, null, null);
        }

        /// <summary>
        /// Returns the field identified by the specified metadata token, in the context defined by the specified generic parameters.
        /// </summary>
        /// <remarks>Generic parameters are ignored in this implementation.</remarks>
        public FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (table.ContainsKey(metadataToken)) return table[metadataToken] as FieldInfo;
            else return null;
        }

        /// <summary>
        /// Returns the field identified by the specified metadata token.
        /// </summary>
        public FieldInfo ResolveField(int metadataToken)
        {
            return this.ResolveField(metadataToken, null, null);
        }

        /// <summary>
        /// Returns the type or member identified by the specified metadata token, in the context defined by the specified 
        /// generic parameters.
        /// </summary>
        /// <remarks>Generic parameters are ignored in this implementation.</remarks>
        public MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (table.ContainsKey(metadataToken)) return table[metadataToken];
            else return null;
        }

        /// <summary>
        /// Returns the type or member identified by the specified metadata token.
        /// </summary>
        public MemberInfo ResolveMember(int metadataToken)
        {
            return this.ResolveMember(metadataToken, null, null);
        }

        /// <summary>
        /// Returns the signature blob identified by a metadata token (not implemented).
        /// </summary>
        /// <exception cref="System.NotImplementedException">This implementation always throws</exception>
        /// <returns>An array of bytes representing the signature blob.</returns>
        public byte[] ResolveSignature(int metadataToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the string identified by the specified metadata token (not implemented).
        /// </summary>
        /// <returns>This implementation always returns <c>null</c></returns>
        public string ResolveString(int metadataToken)
        {
            return null; //not implemented
        }

        /// <summary>
        /// Gets the collection of all members defined in this assembly
        /// </summary>
        public IEnumerable<MemberInfo> EnumerateMembers()
        {
            foreach (MemberInfo member in table.Values)
            {
                yield return member;
            }
        }

        /// <summary>
        /// Gets the collection of all methods defined in this assembly
        /// </summary>
        public IEnumerable<MethodBase> EnumerateMethods()
        {
            foreach (MemberInfo member in table.Values)
            {
                if (member is MethodBase) yield return (MethodBase)member;
            }
        }

        /// <summary>
        /// Gets the types defined in this assembly.
        /// </summary>
        /// <returns>An array that contains all the types that are defined in this assembly.</returns>
        public override Type[] GetTypes()
        {
            List<Type> types = new List<Type>();

            foreach (MemberInfo member in table.Values)
            {
                if (member is Type) types.Add( (Type)member );
            }

            return types.ToArray();
        }

        /// <summary>
        /// Gets the <c>Type</c> object that represents the specified type.
        /// </summary>
        /// <param name="name">The full name of the type.</param>
        /// <returns>An object that represents the specified type, or <c>null</c> if the type is not found.</returns>
        public override Type GetType(string name)
        {
            return GetType(name, false, false);
        }

        /// <summary>
        /// Gets the <c>Type</c> object with the specified name in the assembly instance and optionally throws an exception if the type is not found.
        /// </summary>
        /// <param name="name">The full name of the type.</param>
        /// <param name="throwOnError">true to throw an exception if the type is not found; false to return null.</param>
        /// <returns>An object that represents the specified type.</returns>
        public override Type GetType(string name, bool throwOnError)
        {
            return GetType(name, throwOnError, false);
        }

        /// <summary>
        /// Gets the <c>Type</c> object with the specified name in the assembly instance, with the options of ignoring the case, and of throwing an exception if the type is not found.
        /// </summary>
        /// <param name="name">The full name of the type.</param>
        /// <param name="throwOnError">true to throw an exception if the type is not found; false to return null.</param>
        /// <param name="ignoreCase">true to ignore the case of the type name; otherwise, false.</param>
        /// <returns>An object that represents the specified type.</returns>
        public override Type GetType(string name, bool throwOnError, bool ignoreCase)
        {
            StringComparison comp;

            if (ignoreCase) comp = StringComparison.InvariantCultureIgnoreCase;
            else comp = StringComparison.InvariantCulture;

            foreach (MemberInfo member in table.Values)
            {
                if (member is Type)
                {
                    Type t = (Type)member;

                    if (String.Equals(t.FullName, name, comp)) return t;
                }
            }

            if (throwOnError) throw new TypeLoadException("Type " + name + " not found");
            else return null;
        }

        /// <summary>
        /// Gets a collection of the public types defined in this assembly that are visible outside the assembly.
        /// </summary>
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

        /// <summary>
        /// Gets the public types defined in this assembly that are visible outside the assembly.
        /// </summary>
        /// <returns>An array that represents the types defined in this assembly that are visible outside the assembly.</returns>
        public override Type[] GetExportedTypes()
        {
            List<Type> ret = new List<Type>();

            foreach (Type t in ExportedTypes)
            {
                ret.Add(t);
            }

            return ret.ToArray();
        }

        /// <summary>
        /// Gets a value that indicates whether the current assembly was generated dynamically at runtime by using reflection emit.
        /// </summary>
        public override bool IsDynamic
        {
            get
            {
                if (this.module == null) return false;
                else return this.module.IsDynamic;
            }
        }

        /// <summary>
        /// Gets a boolean value indicating whether this assembly was loaded into the reflection-only context.
        /// </summary>
        /// <value>This implementation always returns <c>true</c></value>
        public override bool ReflectionOnly{ get { return true; } }

         
    }
}
