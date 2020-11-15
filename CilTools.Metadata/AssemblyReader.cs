/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace CilTools.Metadata
{
    /// <summary>
    /// Reads information about .NET assemblies without loading them into the current process
    /// </summary>
    /// <remarks>
    /// <para>When the library needs to resolve metadata tokens from the external assembly, it will attempt to 
    /// read it using <c>AssemblyReader</c>. The <c>AssemblyReader</c> acts as a cache that stores 
    /// assembly instances so we don't need to load them multiple times.</para>
    /// <para>
    /// This type implements the <see cref="IDisposable"/> interface. Calling <see cref="Dispose"/> 
    /// releases all loaded assemblies. It is required to call <see cref="Dispose"/> when you no 
    /// longer need the instance of the <c>AssemblyReader</c>; there's no finalizer. 
    /// </para>
    /// </remarks>
    public sealed class AssemblyReader : IDisposable
    {
        enum AssemblyIdKind
        {
            Name=1, //assembly identified by name
            Path=2  //assembly identified by full file path
        }

        struct AssemblyId
        {
            AssemblyIdKind kind;
            string val;

            public AssemblyId(AssemblyIdKind k, string v)
            {
                this.kind = k;this.val = v;
            }

            public override int GetHashCode()
            {
                int res = 0;

                if (val != null) res = val.GetHashCode();

                unchecked
                {
                    res = res * 11 + (int)kind * 34;
                }

                return res;
            }
        }

        Dictionary<AssemblyId, MetadataAssembly> _assemblies = new Dictionary<AssemblyId, MetadataAssembly>();

        void SetAssembly(ref AssemblyId key, MetadataAssembly val)
        {
            if (this._assemblies.ContainsKey(key)) return;

            this._assemblies[key] = val;
        }

        MetadataAssembly GetAssembly(ref AssemblyId key)
        {
            if (!this._assemblies.ContainsKey(key)) return null;

            return this._assemblies[key];
        }

        /// <summary>
        /// Loads the assembly identified by assembly file path
        /// </summary>
        /// <param name="path">The path to assembly PE file to load</param>
        /// <returns>The assembly object or null if the assembly reader failed to read 
        /// the requested assembly.</returns>
        /// <remarks>
        /// <para>If the assembly is read successfully, it is saved to cache. Eventual attempts to read assembly 
        /// with the same path will fetch it from the cache instead of loading from file again.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Path is null</exception>
        /// <exception cref="ArgumentException">Path is empty string</exception>
        public Assembly LoadFrom(string path)
        {
            if (_assemblies == null) throw new ObjectDisposedException("AssemblyReader");
            if (path == null) throw new ArgumentNullException(path);
            if (path == String.Empty) throw new ArgumentException("Assembly path should not be empty string", path);

            MetadataAssembly ret = null;
            AssemblyId assid = new AssemblyId(AssemblyIdKind.Path, path);
            ret = this.GetAssembly(ref assid); //try cache first

            if (ret != null) return ret;

            ret = new MetadataAssembly(path,this);

            if (ret != null) this.SetAssembly(ref assid, ret); //save to cache

            return ret;
        }

        /// <summary>
        /// Raised when the assembly reader needs to resolve an external assembly
        /// </summary>
        /// <remarks>
        /// When the assembly reader needs to resolve an external assembly, it first looks for it in the 
        /// current runtime directory (where the system assemblies are located). If the assembly is not 
        /// found there, the reader calls the <c>AssemblyResolve</c> event handle so you can customize 
        /// how assemlies are resolved. The rules for handling this event are similar with the 
        /// <see cref="AppDomain.AssemblyResolve"/> event in .NET Framework. See 
        /// <see href="https://docs.microsoft.com/dotnet/standard/assembly/resolve-loads"/> for more 
        /// information.
        /// </remarks>
        public event ResolveEventHandler AssemblyResolve;

        internal Assembly OnResolve(object sender, ResolveEventArgs e)
        {
            AssemblyName n = new AssemblyName(e.Name);

            //try from runtime directory first
            string sname = n.Name;
            string path = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), sname + ".dll");
            Assembly ret = null;

            if (File.Exists(path))
            {
                ret = this.LoadFrom(path);
            }

            if (ret != null) return ret;

            //try to resolve using custom handler
            ResolveEventHandler handler = AssemblyResolve;

            if (handler != null)
            {
                ret = handler(sender, e);
            }

            return ret;
        }

        /// <summary>
        /// Loads the assembly identified by assembly name string.
        /// </summary>
        /// <param name="name">The full name of the assembly to read</param>
        /// <returns>The assembly object or null if the assembly reader failed to read 
        /// the requested assembly.</returns>
        /// <remarks>
        /// <para>If the assembly is read successfully, it is saved to cache. Eventual attempts to read assembly 
        /// with the same name will fetch it from the cache instead of loading from file again.</para>
        /// <para>The full assembly name, besides the simple name, can contain the assembly version, 
        /// public key token and culture. See 
        /// <see href="https://docs.microsoft.com/dotnet/api/system.reflection.assemblyname"/>  
        /// for more information about full assembly names.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Name is null</exception>
        /// <exception cref="ArgumentException">Name is empty string</exception>
        public Assembly Load(string name)
        {
            if (_assemblies == null) throw new ObjectDisposedException("AssemblyReader");
            if (name == null) throw new ArgumentNullException(name);
            if (name == String.Empty) throw new ArgumentException("Assembly name should not be empty string",name);

            Assembly ret=null;
            AssemblyId assid = new AssemblyId(AssemblyIdKind.Name, name);
            ret = this.GetAssembly(ref assid); //try cache first

            if (ret != null) return ret;

            ret = OnResolve(this, new ResolveEventArgs(name));
            MetadataAssembly mAss = ret as MetadataAssembly;

            if (mAss!=null) this.SetAssembly(ref assid, mAss); //save to cache

            return ret;
        }

        /// <summary>
        /// Loads the assembly identified by the <see cref="AssemblyName"/>
        /// </summary>
        /// <param name="name">The name of assembly to load</param>
        /// <returns>The assembly object or null if the assembly reader failed to read 
        /// the requested assembly.</returns>
        /// <remarks>
        /// If the assembly is read successfully, it is saved to cache. Eventual attempts to read assembly 
        /// with the same name will fetch it from the cache instead of loading from file again.
        /// </remarks>
        public Assembly Load(AssemblyName name)
        {
            return this.Load(name.ToString());
        }

        internal Type LoadType(Type t)
        {
            Assembly ea = t.Assembly;
            if (ea == null) throw new TypeLoadException("Failed to resolve type "+t.ToString());

            Assembly ass;

            //if assembly is a reference to external assembly, resolve it
            if (ea is MetadataAssembly) ass = ea;
            else ass = this.Load(ea.GetName());

            if (ass == null) throw new TypeLoadException("Failed to resolve external assembly " + ea.ToString());

            Type ret = ass.GetType(t.FullName);
            if (ret == null) throw new TypeLoadException("Failed to resolve type " + t.AssemblyQualifiedName);

            return ret;
        }

        /// <summary>
        /// Releases resources associated with the assembly reader instance
        /// </summary>
        /// <remarks>
        /// Calling <c>Dispose</c> releases all loaded assemblies. It is required to call <c>Dispose</c> 
        /// when you no longer need the instance of the <c>AssemblyReader</c>; there's no finalizer.
        /// </remarks>
        public void Dispose()
        {
            if (_assemblies == null) return;

            foreach (AssemblyId key in _assemblies.Keys) _assemblies[key].Dispose();

            _assemblies = null;
        }
    }
}
