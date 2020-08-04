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
    public sealed class AssemblyReader : IDisposable
    {
        Dictionary<string, MetadataAssembly> _assemblies = new Dictionary<string, MetadataAssembly>();

        void SetAssembly(string key, MetadataAssembly val)
        {
            if (this._assemblies.ContainsKey(key)) return;

            this._assemblies[key] = val;
        }

        MetadataAssembly GetAssembly(string key)
        {
            if (!this._assemblies.ContainsKey(key)) return null;

            return this._assemblies[key];
        }

        public Assembly LoadFrom(string path)
        {
            if (_assemblies == null) throw new ObjectDisposedException("AssemblyReader");

            MetadataAssembly ret = new MetadataAssembly(path,this);

            if (ret != null) this.SetAssembly(ret.GetName().ToString(), ret); //save to cache

            return ret;
        }

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

        public Assembly Load(string name)
        {
            if (_assemblies == null) throw new ObjectDisposedException("AssemblyReader");

            Assembly ret=null;
            ret = this.GetAssembly(name); //try cache first

            if (ret != null) return ret;

            ret = OnResolve(this, new ResolveEventArgs(name));
            MetadataAssembly mAss = ret as MetadataAssembly;

            if (mAss!=null) this.SetAssembly(name, mAss); //save to cache

            return ret;
        }

        public Assembly Load(AssemblyName name)
        {
            return this.Load(name.ToString());
        }

        internal Type LoadType(ExternalType et)
        {
            ExternalAssembly ea = et.Assembly as ExternalAssembly;

            if (ea == null) return null;

            Assembly ass = this.Load(ea.GetName());

            if (ass == null) return null;

            return ass.GetType(et.FullName);
        }

        public void Dispose()
        {
            if (_assemblies == null) return;

            foreach (string key in _assemblies.Keys) _assemblies[key].Dispose();

            _assemblies = null;
        }
    }
}
