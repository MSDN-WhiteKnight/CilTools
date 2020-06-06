using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CilTools.BytecodeAnalysis;

namespace CilView
{
    sealed class FileAssemblySource:AssemblySource
    {
        string _path; //assembly directory

        public FileAssemblySource(string filepath)
        {
            this._path = Path.GetDirectoryName(filepath);
            ObservableCollection<Assembly> ret = new ObservableCollection<Assembly>();

            Assembly main=null;

            //try load to execution context
            try 
            { 
                main = Assembly.LoadFile(filepath);
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
            catch (BadImageFormatException) { }

            //if failed, try reflection only context
            if (main == null)
            {                
                main = Assembly.ReflectionOnlyLoadFrom(filepath);
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;
            }
            
            if (main == null) throw new ApplicationException("Cannot load assembly " + filepath + " due to unknown error!");

            ret.Add(main);
            this.Assemblies = ret;
            this.Types = new ObservableCollection<Type>();
            this.Methods = new ObservableCollection<MethodBase>();
            
            //FileLoadException
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName an = new AssemblyName(args.Name);
            string path = Path.Combine(this._path, an.Name + ".dll");
            Assembly ret = null;

            //atempt to resolve assembly from current assembly directory

            try
            {
                if (File.Exists(path)) ret = Assembly.LoadFile(path);
            }
            catch (FileNotFoundException) { }
            catch (FileLoadException) { }
            catch (BadImageFormatException) { }

            if (ret != null) return ret;

            //if failed, resolve by full assembly name
            return Assembly.Load(args.Name);
        }

        Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName an = new AssemblyName(args.Name);
            string path = Path.Combine(this._path, an.Name + ".dll");
            Assembly ret = null;

            //atempt to resolve assembly from current assembly directory

            try
            {
                if (File.Exists(path)) ret = Assembly.ReflectionOnlyLoadFrom(path);
            }
            catch (FileNotFoundException) { }
            catch (FileLoadException) { }
            catch (BadImageFormatException) { }

            if (ret != null) return ret;

            //if failed, resolve by full assembly name
            return Assembly.ReflectionOnlyLoad(args.Name);
        }

        public override void Dispose()
        {
            //just do nothing
        }
    }
}
