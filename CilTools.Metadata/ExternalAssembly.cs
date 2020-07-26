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
    public sealed class ExternalAssembly : Assembly
    {
        AssemblyReference assref;
        AssemblyReferenceHandle hAssRef;
        MetadataAssembly owner;
        AssemblyName asn;

        internal ExternalAssembly(AssemblyReference ar, AssemblyReferenceHandle arh, MetadataAssembly owner)
        {
            this.owner = owner;
            this.assref = ar;
            this.hAssRef = arh;
            AssemblyName n = new AssemblyName();
            
            //read assembly information and fill AssemblyName properties            
            n.Name = owner.MetadataReader.GetString(assref.Name);
            n.Version = assref.Version;            

            StringHandle culture = assref.Culture;
            if (!culture.IsNil) n.CultureInfo = new CultureInfo(owner.MetadataReader.GetString(culture));

            BlobHandle key = assref.PublicKeyOrToken;
            if (!key.IsNil)
            {
                if (assref.Flags.HasFlag(AssemblyFlags.PublicKey))
                {
                    n.SetPublicKey(owner.MetadataReader.GetBlobBytes(key));
                    n.Flags |= AssemblyNameFlags.PublicKey;
                }
                else n.SetPublicKeyToken(owner.MetadataReader.GetBlobBytes(key));
            }

            n.CodeBase = "";                     
            this.asn = n;
        }

        public MetadataAssembly Owner { get { return this.owner; } }
        
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
            yield break;
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
    }
}
