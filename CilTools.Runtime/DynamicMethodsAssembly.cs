/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CilTools.BytecodeAnalysis;
using Microsoft.Diagnostics.Runtime;

namespace CilTools.Runtime
{
    /// <summary>
    /// Represents the pseudo-assembly that contains a collection of dynamic methods in the external process
    /// </summary>
    public sealed class DynamicMethodsAssembly : Assembly,IDisposable
    {
        DataTarget target; //null indicates that object is disposed
        AssemblyName asn;
        DynamicMethodsType type;
        bool ownsTarget; //if true, automatically dispose DataTarget on Dispose()
        ClrAssemblyReader reader;

        /// <summary>
        /// Creates new <c>DynamicMethodsAssembly</c> object for the specified data target, optionally indicating whether the created object 
        /// owns the data target.
        /// </summary>
        /// <param name="dt">ClrMD data target to read information from</param>
        /// <param name="autoDispose">
        /// <c>true</c> if this instance owns tha data target and should dispose it automatically when it is no longer needed
        /// </param>
        /// <exception cref="System.ArgumentNullException">Supplied data target is null</exception>
        public DynamicMethodsAssembly(DataTarget dt, bool autoDispose):this(dt,null,autoDispose)
        {
            
        }

        /// <summary>
        /// Creates new <c>DynamicMethodsAssembly</c> object for the specified data target, optionally 
        /// providing the <see cref="ClrAssemblyReader"/> instance and indicating whether the created object 
        /// owns the data target.
        /// </summary>
        /// <param name="dt">ClrMD data target to read information from</param>
        /// <param name="r">
        /// Assembly reader object to read dependent assemblies.
        /// </param>
        /// <param name="autoDispose">
        /// <c>true</c> if this instance owns tha data target and should dispose it automatically when it is no longer needed
        /// </param>
        /// <remarks>
        /// If the supplied <see cref="ClrAssemblyReader"/> is null, the constructor will create new 
        /// assembly reader instance using the supplied data target. 
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">Supplied data target is null</exception>
        public DynamicMethodsAssembly(DataTarget dt, ClrAssemblyReader r, bool autoDispose)
        {
            if (dt == null) throw new ArgumentNullException("dt");

            if (r == null)
            {
                r = new ClrAssemblyReader(dt.ClrVersions[0].CreateRuntime());
            }

            this.target = dt;
            this.ownsTarget = autoDispose;
            this.type = new DynamicMethodsType(this);
            AssemblyName n = new AssemblyName();
            n.Name = "<Process" + dt.ProcessId.ToString() + ".DynamicMethods>";
            n.CodeBase = "";
            this.asn = n;
            this.reader = r;
        }

        /// <summary>
        /// Gets pseudo-type that contains the collection of dynamic methods
        /// </summary>
        public Type ChildType { get { return this.type; } }

        /// <summary>
        /// ClrMD data target associated with this instance
        /// </summary>
        public DataTarget Target
        {
            get { return this.target; }
        }

        /// <summary>
        /// Gets the assembly reader object used to read dependent assemblies
        /// </summary>
        /// <remarks>
        /// When dynamic method references a member from the external assembly, the library will 
        /// use this assembly reader object when resolving metadata tokens from the external assembly. 
        /// The assembly reader acts as cache that stores assembly instance so we don't need to load 
        /// them multiple times.
        /// </remarks>
        public ClrAssemblyReader AssemblyReader
        {
            get { return this.reader; }
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
        /// Gets the full path to the assembly. This implementation always returns an empty string.
        /// </summary>
        public override string Location
        {
            get
            {
                return "";
            }
        }

        /// <summary>
        /// Gets the full path to the assembly. This implementation always returns an empty string.
        /// </summary>
        public override string CodeBase
        {
            get
            {
                return "";
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the current assembly was generated dynamically at runtime.
        /// </summary>
        /// <remarks>This implementation always returns <c>true</c></remarks>
        public override bool IsDynamic
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a boolean value indicating whether this assembly was loaded into the reflection-only context.
        /// </summary>
        /// <value>This implementation always returns <c>true</c></value>
        public override bool ReflectionOnly { get { return true; } }

        /// <summary>
        /// Gets the collection of all methods defined in this assembly
        /// </summary>
        public IEnumerable<MethodBase> EnumerateMethods()
        {
            if (this.target == null) throw new ObjectDisposedException("DataTarget");

            MethodBase[] ret=this.reader.GetDynamicMethodsArray();

            for (int i=0;i<ret.Length;i++)
            {
                yield return ret[i];
            }
        }

        /// <summary>
        /// Gets the types defined in this assembly. 
        /// </summary>
        /// <returns>An array that contains all the types that are defined in this assembly.</returns>
        /// <remarks>This implementation returns collection that consists of the single pseudo-type representing dynamic methods</remarks>
        public override Type[] GetTypes()
        {
            return new Type[] { this.type };
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

            if (String.Equals(name, DynamicMethodsType.TypeName, comp))
            {
                return this.type;
            }
            else
            {
                if (throwOnError) throw new TypeLoadException("Type " + name + " not found");
                else return null;
            }
        }

        /// <summary>
        /// Gets a collection of the public types defined in this assembly that are visible outside the assembly.
        /// </summary>
        /// <remarks>This implementation returns an empty collection</remarks>
        public override IEnumerable<Type> ExportedTypes
        {
            get
            {
                return this.GetExportedTypes();
            }
        }

        /// <summary>
        /// Gets the public types defined in this assembly that are visible outside the assembly.
        /// </summary>
        /// <returns>An array that represents the types defined in this assembly that are visible outside the assembly.</returns>
        /// <remarks>This implementation returns an empty array</remarks>
        public override Type[] GetExportedTypes()
        {
            return new Type[] { };
        }

        /// <summary>
        /// Gets an entry point method for this assembly
        /// </summary>
        /// <value>This implementation always returns null</value>
        public override MethodInfo EntryPoint => null;

        /// <summary>
        /// Gets an array of assembly names for assemblies referenced by this assembly
        /// </summary>
        /// <returns>This implementation always returns an empty array</returns>
        public override AssemblyName[] GetReferencedAssemblies()
        {
            //this is useless, but we override to prevent NotImplementedException popping up
            return new AssemblyName[0];
        }

        /// <summary>
        /// Gets a module that contains the assembly manifest
        /// </summary>
        /// <returns>This implementation always returns null</returns>
        public override Module ManifestModule => null;

        /// <summary>
        /// Releases unmagnaged resources associated with this assembly object
        /// </summary>
        /// <remarks>
        /// This method only disposes the underlying data target if this instance was constructed with the <c>autoDispose</c> 
        /// parameter set to true.
        /// </remarks>
        public void Dispose()
        {
            if (this.target == null) return; //already disposed

            //dispose underlying data target, if we own it

            if (this.ownsTarget)
            {
                this.target.Dispose();
            }

            this.target = null;
        }
    }
}
