/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata.Ecma335;
using System.Globalization;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;

namespace CilTools.Metadata
{
    public sealed class MetadataAssembly : Assembly, ITokenResolver, IDisposable
    {
        static MetadataAssembly unknown = new MetadataAssembly(null,null);

        internal static MetadataAssembly UnknownAssembly
        {
            get { return unknown; }
        }

        MetadataReader reader;
        PEReader peReader;
        AssemblyName asn;
        AssemblyReader assreader;
        Dictionary<int, MemberInfo> cache = new Dictionary<int, MemberInfo>();

        internal MetadataAssembly(string path, AssemblyReader ar)
        {
            this.assreader = ar;
            AssemblyName n = new AssemblyName();

            if (path == null) //unknown assembly
            {
                n.Name = "???";
                n.CodeBase = "";
                this.asn = n;
                return;
            }

            //open PE file
            FileStream s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            PEReader pr = new PEReader(s);

            //create MetadataReader from PE file
            this.reader = pr.GetMetadataReader();
            this.peReader = pr;
            //stream is disposed by PEReader.Dispose, no need to store it

            //read assembly information and fill AssemblyName properties
            AssemblyDefinition adef = this.reader.GetAssemblyDefinition();
            n.Name = reader.GetString(adef.Name);
            n.Version = adef.Version;
            n.HashAlgorithm = (System.Configuration.Assemblies.AssemblyHashAlgorithm)adef.HashAlgorithm;

            StringHandle culture = adef.Culture;
            if (!culture.IsNil) n.CultureInfo = new CultureInfo(reader.GetString(culture));

            BlobHandle key = adef.PublicKey;
            if (!key.IsNil)
            {
                n.SetPublicKey(reader.GetBlobBytes(key));
                n.Flags |= AssemblyNameFlags.PublicKey;
            }

            n.CodeBase = path;
            this.asn = n;
        }

        MemberInfo CacheGetValue(int token)
        {
            if (!this.cache.ContainsKey(token)) return null;
            else return this.cache[token];
        }

        public MetadataReader MetadataReader { get { return this.reader; } }

        public PEReader PEReader { get { return this.peReader; } }

        public AssemblyReader AssemblyReader { get { return this.assreader; } }

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

        /// <summary>
        /// Returns the type identified by the specified metadata token, in the context defined by the specified generic parameters.
        /// </summary>
        /// <remarks>Generic parameters are ignored in this implementation.</remarks>
        public Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (this.reader == null) return null;

            EntityHandle eh = MetadataTokens.EntityHandle(metadataToken);

            if (eh.IsNil) return null;

            if (eh.Kind == HandleKind.TypeDefinition)
            {
                TypeDefinition tdef = reader.GetTypeDefinition((TypeDefinitionHandle)eh);
                return new MetadataType(tdef, (TypeDefinitionHandle)eh, this);
            }
            else if (eh.Kind == HandleKind.TypeReference)
            {
                TypeReference tref = reader.GetTypeReference((TypeReferenceHandle)eh);
                return new ExternalType(tref, (TypeReferenceHandle)eh, this);
            }
            else if (eh.Kind == HandleKind.TypeSpecification)
            {
                TypeSpecification tspec = reader.GetTypeSpecification((TypeSpecificationHandle)eh);
                byte[] bytes = this.reader.GetBlobBytes(tspec.Signature);
                MethodBase declaringMethod = null;

                if (genericMethodArguments != null && genericMethodArguments.Length > 0)
                {
                    declaringMethod = genericMethodArguments[0].DeclaringMethod;

                    //we need non-null value here to indicate that this type is a generic method argument
                    if (declaringMethod == null) declaringMethod = UnknownMethod.Value;
                }

                TypeSpec decoded = TypeSpec.ReadFromArray(bytes, this, declaringMethod);

                if (decoded != null) return decoded.Type;
                else return null;
            }
            else return null;
        }

        /// <summary>
        /// Returns the type identified by the specified metadata token.
        /// </summary>
        public Type ResolveType(int metadataToken)
        {
            Type t = this.CacheGetValue(metadataToken) as Type;
            if (t != null) return t;

            t = this.ResolveType(metadataToken, null, null);
            if (t != null) this.cache[metadataToken] = t;
            return t;
        }

        /// <summary>
        /// Returns the method or constructor identified by the specified metadata token, in the context defined by the 
        /// specified generic parameters.
        /// </summary>
        /// <remarks>Generic parameters are ignored in this implementation.</remarks>
        public MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (this.reader == null) return null;

            EntityHandle eh = MetadataTokens.EntityHandle(metadataToken);

            if (eh.IsNil) return null;

            if (eh.Kind == HandleKind.MethodDefinition)
            {
                MethodDefinition mdef = reader.GetMethodDefinition((MethodDefinitionHandle)eh);
                return new MetadataMethod(mdef, (MethodDefinitionHandle)eh, this);
            }
            else if (eh.Kind == HandleKind.MemberReference)
            {
                MemberReference mref = reader.GetMemberReference((MemberReferenceHandle)eh);

                if (mref.GetKind() == MemberReferenceKind.Method)
                    return new ExternalMethod(mref, (MemberReferenceHandle)eh, this);
                else
                    return null;
            }
            else if (eh.Kind == HandleKind.MethodSpecification)
            {
                MethodSpecification mspec = reader.GetMethodSpecification((MethodSpecificationHandle)eh);
                return new MethodInstance(mspec, (MethodSpecificationHandle)eh, this);
            }
            else return null;
        }

        /// <summary>
        /// Returns the method or constructor identified by the specified metadata token.
        /// </summary>
        public MethodBase ResolveMethod(int metadataToken)
        {
            MethodBase m = this.CacheGetValue(metadataToken) as MethodBase;
            if (m != null) return m;

            m = this.ResolveMethod(metadataToken, null, null);
            if (m != null) this.cache[metadataToken] = m;
            return m;
        }

        /// <summary>
        /// Returns the field identified by the specified metadata token, in the context defined by the specified generic parameters.
        /// </summary>
        /// <remarks>Generic parameters are ignored in this implementation.</remarks>
        public FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (this.reader == null) return null;

            EntityHandle eh = MetadataTokens.EntityHandle(metadataToken);

            if (eh.IsNil) return null;

            if (eh.Kind == HandleKind.FieldDefinition)
            {
                FieldDefinition field = reader.GetFieldDefinition((FieldDefinitionHandle)eh);
                return new MetadataField(field, (FieldDefinitionHandle)eh, this);
            }
            else if (eh.Kind == HandleKind.MemberReference)
            {
                MemberReference mref = reader.GetMemberReference((MemberReferenceHandle)eh);

                if (mref.GetKind() == MemberReferenceKind.Field)
                    return new ExternalField(mref, (MemberReferenceHandle)eh, this);
                else
                    return null;
            }
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
            if (this.reader == null) return null;
            MemberInfo m;

            if (genericTypeArguments == null && genericMethodArguments == null)
            {
                //if there's no generic context, we can read value from cache
                m = this.CacheGetValue(metadataToken);
                if (m != null) return m;
            }

            EntityHandle eh = MetadataTokens.EntityHandle(metadataToken);

            if (eh.IsNil) return null;

            if (eh.Kind == HandleKind.MethodDefinition)
            {
                MethodDefinition mdef = reader.GetMethodDefinition((MethodDefinitionHandle)eh);
                m = new MetadataMethod(mdef, (MethodDefinitionHandle)eh, this);
            }
            else if (eh.Kind == HandleKind.FieldDefinition)
            {
                FieldDefinition field = reader.GetFieldDefinition((FieldDefinitionHandle)eh);
                m = new MetadataField(field, (FieldDefinitionHandle)eh, this);
            }
            else if (eh.Kind == HandleKind.MemberReference)
            {
                MemberReference mref = reader.GetMemberReference((MemberReferenceHandle)eh);

                if (mref.GetKind() == MemberReferenceKind.Method)
                    m = new ExternalMethod(mref, (MemberReferenceHandle)eh, this);
                else if (mref.GetKind() == MemberReferenceKind.Field)
                    m = new ExternalField(mref, (MemberReferenceHandle)eh, this);
                else
                    m = null;
            }
            else if (eh.Kind == HandleKind.TypeDefinition)
            {
                TypeDefinition tdef = reader.GetTypeDefinition((TypeDefinitionHandle)eh);
                m = new MetadataType(tdef, (TypeDefinitionHandle)eh, this);
            }
            else if (eh.Kind == HandleKind.TypeReference)
            {
                TypeReference tref = reader.GetTypeReference((TypeReferenceHandle)eh);
                m = new ExternalType(tref, (TypeReferenceHandle)eh, this);
            }
            else if (eh.Kind == HandleKind.MethodSpecification)
            {
                MethodSpecification mspec = reader.GetMethodSpecification((MethodSpecificationHandle)eh);
                m = new MethodInstance(mspec, (MethodSpecificationHandle)eh, this);
            }
            else if (eh.Kind == HandleKind.TypeSpecification)
            {
                TypeSpecification tspec = reader.GetTypeSpecification((TypeSpecificationHandle)eh);
                byte[] bytes = this.reader.GetBlobBytes(tspec.Signature);
                MethodBase declaringMethod = null;

                if (genericMethodArguments != null && genericMethodArguments.Length > 0)
                {
                    declaringMethod = genericMethodArguments[0].DeclaringMethod;

                    //we need non-null value here to indicate that this type is a generic method argument
                    if (declaringMethod == null) declaringMethod = UnknownMethod.Value;
                }

                TypeSpec decoded = TypeSpec.ReadFromArray(bytes, this, declaringMethod);

                if (decoded != null) m = decoded.Type;
                else m = null;
            }
            else m = null;

            if (m!=null && genericTypeArguments == null && genericMethodArguments == null)
            {
                //if there's no generic context, store value in cache
                this.cache[metadataToken] = m;
            }

            return m;
        }

        /// <summary>
        /// Returns the type or member identified by the specified metadata token.
        /// </summary>
        public MemberInfo ResolveMember(int metadataToken)
        {
            MemberInfo m = this.CacheGetValue(metadataToken);

            if (m != null) return m;
            else return this.ResolveMember(metadataToken, null, null);
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
        /// Returns the string identified by the specified metadata token.
        /// </summary> 
        public string ResolveString(int metadataToken)
        {
            if (this.reader == null) return null;

            Handle h = MetadataTokens.Handle(metadataToken);

            if (h.IsNil) return null;

            if (h.Kind == HandleKind.String)
            {
                return reader.GetString((StringHandle)h);
            }
            else if (h.Kind == HandleKind.UserString)
            {
                return reader.GetUserString((UserStringHandle)h);
            }
            else return null;
        }

        /// <summary>
        /// Gets the collection of all members defined in this assembly
        /// </summary>
        public IEnumerable<MemberInfo> EnumerateMembers()
        {
            if (this.reader == null) yield break;

            foreach (MethodDefinitionHandle mdefh in reader.MethodDefinitions)
            {
                yield return this.GetMethodDefinition(mdefh);
            }

            foreach (TypeDefinitionHandle ht in reader.TypeDefinitions)
            {
                yield return this.GetTypeDefinition(ht);
            }

            foreach (FieldDefinitionHandle hfield in reader.FieldDefinitions)
            {
                FieldDefinition field = reader.GetFieldDefinition(hfield);
                yield return new MetadataField(field, hfield, this);
            }
        }

        /// <summary>
        /// Gets the collection of all methods defined in this assembly
        /// </summary>
        public IEnumerable<MethodBase> EnumerateMethods()
        {
            if (this.reader == null) yield break;

            foreach (MethodDefinitionHandle mdefh in reader.MethodDefinitions)
            {
                yield return this.GetMethodDefinition(mdefh);
            }
        }

        internal Type GetTypeDefinition(TypeDefinitionHandle ht)
        {
            int token = this.MetadataReader.GetToken(ht);

            return this.ResolveType(token);
        }

        internal MethodBase GetMethodDefinition(MethodDefinitionHandle hm)
        {
            int token = this.MetadataReader.GetToken(hm);

            return this.ResolveMethod(token);
        }

        /// <summary>
        /// Gets the types defined in this assembly.
        /// </summary>
        /// <returns>An array that contains all the types that are defined in this assembly.</returns>
        public override Type[] GetTypes()
        {
            if (this.reader == null) return new Type[0];

            List<Type> ret = new List<Type>();

            foreach (TypeDefinitionHandle ht in reader.TypeDefinitions)
            {
                ret.Add(this.GetTypeDefinition(ht));
            }

            return ret.ToArray();
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

        string GetTypeFullName(TypeDefinition tdef)
        {
            StringBuilder sb = new StringBuilder(500);
            string ns = this.reader.GetString(tdef.Namespace);
            if (String.IsNullOrEmpty(ns))
            {
                TypeDefinitionHandle declh = tdef.GetDeclaringType();
                if (!declh.IsNil)
                {
                    //nested type
                    TypeDefinition decl = this.reader.GetTypeDefinition(declh);
                    ns = this.reader.GetString(decl.Namespace);
                    sb.Append(ns);
                    sb.Append('.');
                    sb.Append(this.reader.GetString(decl.Name));
                    sb.Append('+');
                }
            }
            else
            {
                sb.Append(ns);
                sb.Append('.');
            }

            string typename = reader.GetString(tdef.Name);
            sb.Append(typename);

            return sb.ToString();
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
            if (this.reader == null) return null;

            StringComparison cmp;

            if (ignoreCase) cmp = StringComparison.InvariantCultureIgnoreCase;
            else cmp = StringComparison.InvariantCulture;

            foreach (TypeDefinitionHandle ht in reader.TypeDefinitions)
            {
                TypeDefinition tdef = reader.GetTypeDefinition(ht);
                string typename = GetTypeFullName(tdef);

                if (String.Equals(name, typename, cmp)) return this.GetTypeDefinition(ht);
            }

            //if not found definition, search for type forwarders...
            foreach (ExportedTypeHandle eth in reader.ExportedTypes)
            {
                ExportedType et = reader.GetExportedType(eth);
                if (!et.IsForwarder) continue;

                string typename = reader.GetString(et.Namespace) + "." + reader.GetString(et.Name);
                if (!String.Equals(name, typename, cmp)) continue;
                
                EntityHandle h = et.Implementation;
                if (h.IsNil) continue;
                if (h.Kind != HandleKind.AssemblyReference) continue;

                ExternalAssembly ea = new ExternalAssembly(
                    reader.GetAssemblyReference((AssemblyReferenceHandle)h), (AssemblyReferenceHandle)h, this
                    );

                Assembly ass = assreader.Load(ea.GetName());
                Type ret = ass.GetType(name, throwOnError, ignoreCase);
                if (ret != null) return ret;
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
                if (this.reader == null) yield break;

                foreach (TypeDefinitionHandle ht in reader.TypeDefinitions)
                {
                    TypeDefinition tdef = reader.GetTypeDefinition(ht);

                    if (tdef.Attributes.HasFlag(TypeAttributes.Public))
                        yield return this.GetTypeDefinition(ht);
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
                return false;                
            }
        }

        /// <summary>
        /// Gets a boolean value indicating whether this assembly was loaded into the reflection-only context.
        /// </summary>
        /// <value>This implementation always returns <c>true</c></value>
        public override bool ReflectionOnly { get { return true; } }

        public void Dispose()
        {
            if (this.peReader != null)
            {
                this.peReader.Dispose();
                this.reader = null;
                this.peReader = null;
            }
        }
    }
}

