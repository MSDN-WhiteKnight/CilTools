/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using CilTools.BytecodeAnalysis;
using CilTools.Metadata;

namespace CilView
{
    sealed class FileAssemblySource:AssemblySource
    {
        string _path; //assembly directory
        AssemblyReader rd;

        Assembly LoadAssembly(string filepath)
        {
            Assembly main = null;
            
            main = rd.LoadFrom(filepath);

            if (main != null)
            {
                return main;
            }
            else return null;
        }

        private Assembly Rd_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName an = new AssemblyName(args.Name);
            string path = Path.Combine(this._path, an.Name + ".dll");
            Assembly ret = null;

            //attempt to resolve assembly from current assembly directory

            try
            {
                if (File.Exists(path)) ret = this.rd.LoadFrom(path);
            }
            catch (FileNotFoundException) { }            
            catch (BadImageFormatException) { }
            catch (InvalidOperationException) { }

            return ret;
        }

        public FileAssemblySource(string filepath)
        {
            AssemblySource.TypeCacheClear();

            this._path = Path.GetDirectoryName(filepath);
            ObservableCollection<Assembly> ret = new ObservableCollection<Assembly>();

            Assembly main=null;
            this.rd = new AssemblyReader();
            rd.AssemblyResolve += Rd_AssemblyResolve;
            main = this.LoadAssembly(filepath);
            
            if (main == null) throw new ApplicationException("Cannot load assembly " + filepath + " due to unknown error!");

            ret.Add(main);
            this.Assemblies = ret;
            this.Types = new ObservableCollection<Type>();
            this.Methods = new ObservableCollection<MethodBase>();
        }

        public override bool HasProcessInfo
        {
            get { return false; }
        }

        public override string GetProcessInfoString()
        {
            return "";
        }

        public override CilTools.Runtime.ClrThreadInfo[] GetProcessThreads()
        {
            return new CilTools.Runtime.ClrThreadInfo[0];
        }

        public override void Dispose()
        {
            this.Methods = new ObservableCollection<MethodBase>();
            this.Types = new ObservableCollection<Type>();
            this.Assemblies = new ObservableCollection<Assembly>();
            this.rd.Dispose();
        }
    }
}
