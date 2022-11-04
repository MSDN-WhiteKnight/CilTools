/* CIL Tools 
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using CilTools.BytecodeAnalysis;
using CilTools.Internal;
using CilTools.Metadata.Methods;
using CilTools.Metadata.PortableExecutable;
using CilTools.Reflection;

namespace CilTools.Metadata
{
    /// <summary>
    /// Represents the metadata of the .NET assembly. Using this class enables you to inspect assembly 
    /// metadata witout loading into any context of the current process.
    /// </summary>
    /// <remarks>
    /// This type implements <see cref="IDisposable"/>. However, calling <see cref="Dispose"/> is not required 
    /// when the instance is owned by the <see cref="CilTools.Metadata.AssemblyReader"/> and the owning 
    /// reader was disposed.
    /// </remarks>
    internal sealed class MetadataAssembly : Assembly, ITokenResolver, IDisposable, IReflectionInfo
    {
        static MetadataAssembly unknown = new MetadataAssembly((string)null,null);

        internal static MetadataAssembly UnknownAssembly
        {
            get { return unknown; }
        }

        MetadataReader reader;
        PEReader peReader;
        AssemblyName asn;
        AssemblyReader assreader;
        Dictionary<int, MemberInfo> cache = new Dictionary<int, MemberInfo>();
        bool fromMemory;

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
            this.fromMemory = false;
            Debug.WriteLine("Loaded from file: "+n.Name);
        }

        internal MetadataAssembly(MemoryImage image, AssemblyReader ar)
        {
            this.assreader = ar;
            AssemblyName n = new AssemblyName();

            //open PE image
            Stream s = image.GetStream();
            PEStreamOptions options = PEStreamOptions.Default;
            if (!image.IsFileLayout) options = PEStreamOptions.IsLoadedImage;
            PEReader pr = new PEReader(s,options);

            //create MetadataReader
            this.reader = pr.GetMetadataReader();
            this.peReader = pr;

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

            n.CodeBase = image.FilePath;
            this.asn = n;
            this.fromMemory = true;
            Debug.WriteLine("Loaded from memory: " + n.Name);
        }

        MemberInfo CacheGetValue(int token)
        {
            if (!this.cache.ContainsKey(token)) return null;
            else return this.cache[token];
        }

        /// <summary>
        /// Gets an object used to read .NET metadata of this assembly
        /// </summary>
        public MetadataReader MetadataReader { get { return this.reader; } }

        /// <summary>
        /// Gets an object used to read metadata from PE file
        /// </summary>
        public PEReader PEReader { get { return this.peReader; } }

        /// <summary>
        /// Gets the assembly reader that owns this instance
        /// </summary>
        public AssemblyReader AssemblyReader { get { return this.assreader; } }

        public bool FromMemoryImage
        {
            get { return this.fromMemory; }
        }

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
        /// Gets the full path to the PE file containing this assembly.
        /// </summary>
        public override string Location
        {
            get
            {
                return this.asn.CodeBase;
            }
        }

        /// <summary>
        /// Gets the full path to the PE file containing this assembly.
        /// </summary>
        public override string CodeBase
        {
            get
            {
                return this.asn.CodeBase;
            }
        }

        public override Module ManifestModule
        {
            get
            {
                // There's only one module definition (others are external references), so the handle
                // is just synthesized.
                ModuleDefinitionHandle h = (ModuleDefinitionHandle)MetadataTokens.EntityHandle(TableIndex.Module, 1);
                ModuleDefinition m = this.reader.GetModuleDefinition();
                return new ModuleDef(this, m, h);
            }
        }

        Type ResolveTypeImpl(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments) 
        {
            if (this.reader == null) return null;

            EntityHandle eh = MetadataTokens.EntityHandle(metadataToken);

            if (eh.IsNil) return null;

            Type ret;

            if (genericTypeArguments == null && genericMethodArguments == null)
            {
                //if there's no generic context, we can read value from cache
                ret = this.CacheGetValue(metadataToken) as Type;
                if (ret != null) return ret;
            }

            if (eh.Kind == HandleKind.TypeDefinition)
            {
                TypeDefinition tdef = reader.GetTypeDefinition((TypeDefinitionHandle)eh);
                ret = new TypeDef(tdef, (TypeDefinitionHandle)eh, this);
            }
            else if (eh.Kind == HandleKind.TypeReference)
            {
                TypeReference tref = reader.GetTypeReference((TypeReferenceHandle)eh);
                ret = new TypeRef(tref, (TypeReferenceHandle)eh, this);
            }
            else if (eh.Kind == HandleKind.TypeSpecification)
            {
                TypeSpecification tspec = reader.GetTypeSpecification((TypeSpecificationHandle)eh);
                byte[] bytes = this.reader.GetBlobBytes(tspec.Signature);
                GenericContext gctx = GenericContext.FromArgs(genericTypeArguments, genericMethodArguments);
                SignatureContext ctx = SignatureContext.Create(this, gctx, null);
                TypeSpec decoded = TypeSpec.ReadFromArray(bytes, ctx);

                if (decoded != null) ret = decoded;
                else ret = null;
            }
            else ret = null;

            if (ret != null && genericTypeArguments == null && genericMethodArguments == null)
            {
                //if there's no generic context, store value in cache
                this.cache[metadataToken] = ret;
            }

            return ret;
        }

        /// <summary>
        /// Returns the type identified by the specified metadata token, in the context defined by 
        /// the specified generic parameters.
        /// </summary>
        /// <remarks>Generic  type parameters are ignored in this implementation.</remarks>
        public Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return this.ResolveTypeImpl(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        /// <summary>
        /// Returns the type identified by the specified metadata token.
        /// </summary>
        public Type ResolveType(int metadataToken)
        {
            return this.ResolveTypeImpl(metadataToken, null, null);
        }

        MethodBase ResolveMethodImpl(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (this.reader == null) return null;

            EntityHandle eh = MetadataTokens.EntityHandle(metadataToken);

            if (eh.IsNil) return null;

            MethodBase m = null;

            if (genericTypeArguments == null && genericMethodArguments == null)
            {
                //if there's no generic context, we can read value from cache
                m = this.CacheGetValue(metadataToken) as MethodBase;
                if (m != null) return m;
            }

            if (eh.Kind == HandleKind.MethodDefinition)
            {
                MethodDefinition mdef = reader.GetMethodDefinition((MethodDefinitionHandle)eh);
                m = Utils.CreateMethodFromDefinition(mdef, (MethodDefinitionHandle)eh, this);
            }
            else if (eh.Kind == HandleKind.MemberReference)
            {
                MemberReference mref = reader.GetMemberReference((MemberReferenceHandle)eh);

                if (mref.GetKind() == MemberReferenceKind.Method)
                    m = Utils.CreateMethodFromReference(mref, (MemberReferenceHandle)eh, this);
                else
                    m = null;
            }
            else if (eh.Kind == HandleKind.MethodSpecification)
            {
                MethodSpecification mspec = reader.GetMethodSpecification((MethodSpecificationHandle)eh);
                m = new MethodSpec(mspec, (MethodSpecificationHandle)eh, this);
            }
            else m = null;

            if (m != null && genericTypeArguments == null && genericMethodArguments == null)
            {
                //if there's no generic context, store value in cache
                this.cache[metadataToken] = m;
            }

            return m;
        }

        /// <summary>
        /// Returns the method or constructor identified by the specified metadata token, in the context defined by the 
        /// specified generic parameters.
        /// </summary>
        /// <remarks>Generic parameters are ignored in this implementation.</remarks>
        public MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return this.ResolveMethodImpl(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        /// <summary>
        /// Returns the method or constructor identified by the specified metadata token.
        /// </summary>
        public MethodBase ResolveMethod(int metadataToken)
        {
            return this.ResolveMethodImpl(metadataToken, null, null);
        }

        FieldInfo ResolveFieldImpl(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            if (this.reader == null) return null;

            EntityHandle eh = MetadataTokens.EntityHandle(metadataToken);
            if (eh.IsNil) return null;

            MethodBase declaringMethod = null;
            FieldInfo ret;

            if (genericTypeArguments == null && genericMethodArguments == null)
            {
                //if there's no generic context, we can read value from cache
                ret = this.CacheGetValue(metadataToken) as FieldInfo;
                if (ret != null) return ret;
            }
            else if (genericMethodArguments != null && genericMethodArguments.Length > 0)
            {
                if (genericMethodArguments[0].IsGenericParameter)
                {
                    declaringMethod = genericMethodArguments[0].DeclaringMethod;
                }

                //we need non-null value here to provide generic context
                if (declaringMethod == null) declaringMethod = UnknownMethod.Value;
            }

            if (eh.Kind == HandleKind.FieldDefinition)
            {
                FieldDefinition field = reader.GetFieldDefinition((FieldDefinitionHandle)eh);
                ret = new FieldDef(field, (FieldDefinitionHandle)eh, this, declaringMethod);
            }
            else if (eh.Kind == HandleKind.MemberReference)
            {
                MemberReference mref = reader.GetMemberReference((MemberReferenceHandle)eh);

                if (mref.GetKind() == MemberReferenceKind.Field)
                    ret = new FieldRef(mref, (MemberReferenceHandle)eh, this, declaringMethod);
                else
                    ret = null;
            }
            else ret = null;

            if (ret != null && genericTypeArguments == null && genericMethodArguments == null)
            {
                //if there's no generic context, store value in cache
                this.cache[metadataToken] = ret;
            }

            return ret;
        }

        /// <summary>
        /// Returns the field identified by the specified metadata token, in the context defined by 
        /// the specified generic parameters.
        /// </summary>
        /// <remarks>Generic type parameters are ignored in this implementation.</remarks>
        public FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return this.ResolveFieldImpl(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        /// <summary>
        /// Returns the field identified by the specified metadata token.
        /// </summary>
        public FieldInfo ResolveField(int metadataToken)
        {
            return this.ResolveFieldImpl(metadataToken, null, null);
        }

        /// <summary>
        /// Returns the type or member identified by the specified metadata token, in the context defined by the specified 
        /// generic parameters.
        /// </summary>
        public MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            EntityHandle eh = MetadataTokens.EntityHandle(metadataToken);

            if (eh.IsNil) return null;

            if (eh.Kind == HandleKind.MethodDefinition || eh.Kind == HandleKind.MethodSpecification)
            {
                return this.ResolveMethodImpl(metadataToken, genericTypeArguments, genericMethodArguments);
            }
            else if (eh.Kind == HandleKind.FieldDefinition)
            {
                return this.ResolveFieldImpl(metadataToken, genericTypeArguments, genericMethodArguments);
            }
            else if (eh.Kind == HandleKind.MemberReference)
            {
                MemberReference mref = reader.GetMemberReference((MemberReferenceHandle)eh);

                if (mref.GetKind() == MemberReferenceKind.Method)
                    return this.ResolveMethodImpl(metadataToken, genericTypeArguments, genericMethodArguments);
                else if (mref.GetKind() == MemberReferenceKind.Field)
                    return this.ResolveFieldImpl(metadataToken, genericTypeArguments, genericMethodArguments);
                else
                    return null;
            }
            else if (eh.Kind == HandleKind.TypeDefinition || eh.Kind == HandleKind.TypeReference ||
                eh.Kind == HandleKind.TypeSpecification)
            {
                return this.ResolveTypeImpl(metadataToken, genericTypeArguments, genericMethodArguments);
            }
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
        /// Returns the signature blob identified by a metadata token.
        /// </summary>
        /// <returns>An array of bytes representing the signature blob.</returns>
        public byte[] ResolveSignature(int metadataToken)
        {
            EntityHandle eh = MetadataTokens.EntityHandle(metadataToken);

            if (eh.IsNil) return null;
            if (eh.Kind != HandleKind.StandaloneSignature) return null;

            StandaloneSignature ss = this.reader.GetStandaloneSignature((StandaloneSignatureHandle)eh);
            return reader.GetBlobBytes(ss.Signature);
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
                yield return new FieldDef(field, hfield, this, null);
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

            return this.ResolveTypeImpl(token, null, null);
        }

        internal Type GetTypeByHandle(EntityHandle ht)
        {
            int token = this.MetadataReader.GetToken(ht);

            return this.ResolveTypeImpl(token, null, null);
        }

        internal MethodBase GetMethodDefinition(MethodDefinitionHandle hm)
        {
            int token = this.MetadataReader.GetToken(hm);

            return this.ResolveMethodImpl(token, null, null);
        }

        internal MethodInfo GetMethodByHandle(EntityHandle hm)
        {
            int token = this.MetadataReader.GetToken(hm);

            return this.ResolveMethodImpl(token, null, null) as MethodInfo;
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
        /// Gets the <c>Type</c> object with the specified name in the assembly instance, with the options of 
        /// ignoring the case, and of throwing an exception if the type is not found.
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

                AssemblyRef ea = new AssemblyRef(
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
        /// <returns>
        /// An array that represents the types defined in this assembly that are visible outside the assembly.
        /// </returns>
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
        /// Gets a value that indicates whether the current assembly was generated dynamically at runtime 
        /// by using reflection emit.
        /// </summary>
        /// <remarks>
        /// This implementation always returns <c>false</c>. Dynamic assemblies are not supported by 
        /// this API.
        /// </remarks>
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

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit)
        {
            //we can't instantiate actual attribute objects here
            //so we will create special ICustomAttribute objects that CilTools.BytecodeAnalysis recognizes
            //this is needed to emulate GetCustomAttributesData for .NET Framework 3.5

            CustomAttributeHandleCollection coll = this.reader.GetAssemblyDefinition().GetCustomAttributes();
            return Utils.ReadCustomAttributes(coll, this, this);
        }

        public override MethodInfo EntryPoint
        {
            get
            {
                if (this.peReader == null) return null;

                CorHeader ch = this.peReader.PEHeaders.CorHeader;
                int token = ch.EntryPointTokenOrRelativeVirtualAddress;

                if (token == 0) return null;

                return this.ResolveMethodImpl(token, null, null) as MethodInfo;
            }
        }

        public override AssemblyName[] GetReferencedAssemblies()
        {
            if (this.reader == null) return new AssemblyName[0];

            List<AssemblyName> ret = new List<AssemblyName>(this.reader.AssemblyReferences.Count);

            foreach (AssemblyReferenceHandle h in this.reader.AssemblyReferences)
            {
                AssemblyReference ar = this.reader.GetAssemblyReference(h);
                AssemblyName an = new AssemblyName();
                an.Name = this.reader.GetString(ar.Name);
                an.Version = ar.Version;

                try
                {
                    if (!ar.Culture.IsNil) an.CultureInfo = new CultureInfo(this.reader.GetString(ar.Culture));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Failed to get AssemblyReference culture");
                    Debug.WriteLine(ex.ToString());
                }

                if (ar.Flags.HasFlag(AssemblyFlags.PublicKey))
                {
                    BlobHandle key = ar.PublicKeyOrToken;

                    if (!key.IsNil)
                    {
                        an.SetPublicKey(reader.GetBlobBytes(key));
                        an.Flags |= AssemblyNameFlags.PublicKey;
                    }
                }
                else
                {
                    BlobHandle tok = ar.PublicKeyOrToken;

                    if (!tok.IsNil)
                    {
                        an.SetPublicKeyToken(reader.GetBlobBytes(tok));
                    }
                }

                if (!ar.Flags.HasFlag(AssemblyFlags.DisableJitCompileOptimizer))
                {
                    an.Flags |= AssemblyNameFlags.EnableJITcompileOptimizer;
                }

                if (ar.Flags.HasFlag(AssemblyFlags.EnableJitCompileTracking))
                {
                    an.Flags |= AssemblyNameFlags.EnableJITcompileTracking;
                }

                if (ar.Flags.HasFlag(AssemblyFlags.Retargetable))
                {
                    an.Flags |= AssemblyNameFlags.Retargetable;
                }

                ret.Add(an);
            }

            return ret.ToArray();
        }

        public string[] GetReferencedModules()
        {
            int countModules = this.reader.GetTableRowCount(TableIndex.ModuleRef);

            if (countModules == 0)
            {
                return new string[0];
            }

            string[] ret = new string[countModules];

            for (int i = 1; i <= countModules; i++)
            {
                ModuleReferenceHandle h = MetadataTokens.ModuleReferenceHandle(i);
                ModuleReference mref = this.reader.GetModuleReference(h);
                string name = this.reader.GetString(mref.Name);
                ret[i - 1] = name;
            }

            return ret;
        }

        internal VTable[] GetVTables()
        {
            PEReader pr = this.peReader;
            DirectoryEntry de = pr.PEHeaders.CorHeader.VtableFixupsDirectory;
            int vt_rva = de.RelativeVirtualAddress;
            int vt_size = de.Size;

            //get VTable fixups directory raw bytes
            ImmutableArray<byte> vt_data = pr.GetSectionData(vt_rva).GetContent(0, vt_size);
            List<byte> vt_list = new List<byte>(vt_data);
            MemoryStream ms = new MemoryStream(vt_list.ToArray());
            BinaryReader br = new BinaryReader(ms);
            List<VTable> ret = new List<VTable>();

            //parse VTables from directory data
            while (true)
            {
                if (ms.Position >= ms.Length) break;

                int rva = br.ReadInt32();
                short n_items = br.ReadInt16();
                short type = br.ReadInt16();
                int size;

                if ((type & VTable.COR_VTABLE_64BIT) != 0) size = n_items * 8;
                else size = n_items * 4;

                ImmutableArray<byte> data = pr.GetSectionData(rva).GetContent(0, size);

                VTable vt = new VTable(rva, n_items, type, data);
                ret.Add(vt);
            }

            return ret.ToArray();
        }

        public string GetInfoText()
        {
            StringBuilder sb = new StringBuilder(1000);
            sb.Append("Type: ");

            if (this.peReader.PEHeaders.IsExe) sb.AppendLine("EXE");
            else sb.AppendLine("DLL");

            //PE Header
            PEHeader peh = this.peReader.PEHeaders.PEHeader;
            sb.AppendLine("Subsystem: " + peh.Subsystem.ToString());
            sb.AppendLine("Image base: 0x" + peh.ImageBase.ToString("X"));
            sb.AppendLine("File alignment: " + peh.FileAlignment.ToString());
            sb.AppendLine("Section alignment: " + peh.SectionAlignment.ToString());
            sb.AppendLine("Loaded image size: " + Math.Round(peh.SizeOfImage / 1024.0f, 2).ToString() + " KB");

            sb.Append("OS version: ");
            sb.Append(peh.MajorOperatingSystemVersion.ToString());
            sb.AppendLine("." + peh.MinorOperatingSystemVersion.ToString());

            sb.Append("Linker version: ");
            sb.Append(peh.MajorLinkerVersion.ToString());
            sb.AppendLine("." + peh.MinorLinkerVersion.ToString());

            //COFF Header
            CoffHeader ch = this.peReader.PEHeaders.CoffHeader;
            sb.AppendLine("Machine type: " + ch.Machine.ToString());

            int x = (int)ch.Characteristics;
            string characterstics = Utils.FlagsEnumToString<Characteristics>(x);
            sb.Append("Characteristics: 0x" + x.ToString("X"));
            sb.AppendLine(" (" + characterstics + ")");

            //Cor Header
            CorFlags flags = this.peReader.PEHeaders.CorHeader.Flags;
            string flagsStr = Utils.FlagsEnumToString<CorFlags>((int)flags);
            sb.Append("CorFlags: 0x" + ((uint)flags).ToString("X") + " (");
            sb.Append(flagsStr);
            sb.AppendLine(")");

            //Imported P/Invoke modules
            string[] modules = this.GetReferencedModules();

            if (modules.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("    Referenced unmanaged modules");
                sb.AppendLine();

                for (int i = 0; i < modules.Length; i++)
                {
                    sb.AppendLine(modules[i]);
                }
            }

            return sb.ToString();
        }

        object GetImageProperty(int id)
        {
            PEHeader peh = this.peReader.PEHeaders.PEHeader;
            CorHeader ch = this.peReader.PEHeaders.CorHeader;

            switch (id)
            {
                case ReflectionProperties.ImageBase: return peh.ImageBase;
                case ReflectionProperties.FileAlignment: return peh.FileAlignment;
                case ReflectionProperties.StackReserve: return peh.SizeOfStackReserve;
                case ReflectionProperties.Subsystem: return (int)peh.Subsystem;
                case ReflectionProperties.CorFlags: return (int)ch.Flags;
            }

            return null;
        }

        public object GetReflectionProperty(int id)
        {
            switch (id)
            {
                case ReflectionProperties.InfoText: return this.GetInfoText();
                case ReflectionProperties.ReferencedModules: return this.GetReferencedModules();
                default: return this.GetImageProperty(id);
            }
        }

        /// <summary>
        /// Releases resources associated with this instance
        /// </summary>
        /// <remarks>
        /// Calling <c>Dispose</c> is not required when the instance is owned by the 
        /// <see cref="CilTools.Metadata.AssemblyReader"/> and the owning reader was disposed.
        /// </remarks>
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

