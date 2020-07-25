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
    sealed class MetadataAssembly : Assembly, ITokenResolver, IDisposable
    {
        static MetadataAssembly unknown = new MetadataAssembly(null);

        internal static MetadataAssembly UnknownAssembly
        {
            get { return unknown; }
        }

        MetadataReader reader;
        PEReader peReader;
        AssemblyName asn;

        internal MetadataAssembly(string path)
        {
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

        public MetadataReader MetadataReader { get { return this.reader; } }

        public PEReader PEReader { get { return this.peReader; } }

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
            return null;
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
            if (this.reader == null) return null;

            EntityHandle eh = MetadataTokens.EntityHandle(metadataToken);
            MethodDefinition mdef = reader.GetMethodDefinition((MethodDefinitionHandle)eh);
            return new MetadataMethod(mdef, (MethodDefinitionHandle)eh, this);
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
            return null;
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

            EntityHandle eh = MetadataTokens.EntityHandle(metadataToken);
            if (eh.Kind == HandleKind.MethodDefinition)
            {
                MethodDefinition mdef = reader.GetMethodDefinition((MethodDefinitionHandle)eh);
                return new MetadataMethod(mdef, (MethodDefinitionHandle)eh, this);
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
            yield break;
        }

        /// <summary>
        /// Gets the collection of all methods defined in this assembly
        /// </summary>
        public IEnumerable<MethodBase> EnumerateMethods()
        {
            if (this.reader == null) yield break;

            foreach (MethodDefinitionHandle mdefh in reader.MethodDefinitions)
            {
                MethodDefinition mdef = reader.GetMethodDefinition(mdefh);
                yield return new MetadataMethod(mdef, mdefh, this);
            }
        }

        /// <summary>
        /// Gets the types defined in this assembly.
        /// </summary>
        /// <returns>An array that contains all the types that are defined in this assembly.</returns>
        public override Type[] GetTypes()
        {
            return new Type[0];
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
            return null;
        }

        /// <summary>
        /// Gets a collection of the public types defined in this assembly that are visible outside the assembly.
        /// </summary>
        public override IEnumerable<Type> ExportedTypes
        {
            get
            {
                yield break;
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

